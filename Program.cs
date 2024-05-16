using System.Globalization;
using Newtonsoft.Json;

using System.Text.RegularExpressions;



public class Program
{
    private static Regex reTime = new Regex(@"^D\d{4} (\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}\.\d{6})");
    private static Regex reClOrderId = new Regex(@"\|11=([^|]+)\|");
    private static Regex reAccount = new Regex(@"\|1=([^|]+)\|");
    private static Regex reMatchOrderID = new Regex(@"\|198=([^|]+)\|");

    public class JnetConfirmedOrder
    {
        public string ClOrderId { get; set; }
        public string Account { get; set; }
        public string RecvClientTime { get; set; }
        public string SendMatchTime { get; set; }
        public string RecvMatchTime { get; set; }
        public string FinalReturnTime { get; set; }

        public string OmsCostTime1 { get; set; }
        public string MatchCostTime { get; set; }
        public string OmsCostTime2 { get; set; }

        public string TotalCostTime { get; set; }
    }
    public static bool IsJnetConfirmed(string line)
    {
        // 检查字符串是否包含所有特定的子字符串
        return line.Contains("|35=8|") && line.Contains("|20=2|") && line.Contains("|39=2|") && line.Contains("8=FIX");
    }

    public static JnetConfirmedOrder ParseLine(string line)
    {
        var returnTimeMatches = reTime.Match(line);
        var clOrderIdMatches = reClOrderId.Match(line);
        var accountMatches = reAccount.Match(line);

        if (!returnTimeMatches.Success)
        {
            throw new ArgumentException("Final return time not found");
        }
        if (!clOrderIdMatches.Success)
        {
            throw new ArgumentException("Client order ID not found");
        }
        if (!accountMatches.Success)
        {
            throw new ArgumentException("Account not found");
        }

        return new JnetConfirmedOrder
        {
            FinalReturnTime = returnTimeMatches.Groups[1].Value,
            ClOrderId = clOrderIdMatches.Groups[1].Value,
            Account = accountMatches.Groups[1].Value
        };
    }
    public static Dictionary<string, JnetConfirmedOrder> GetAllJnetConfirmedOrders(string filename)
    {
        var orders = new Dictionary<string, JnetConfirmedOrder>();

        StreamReader file;
        try
        {
            file = new StreamReader(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file: {ex.Message}");
            return null;  // 返回null表示无法打开文件
        }

        int count = 0;  // 在try-catch外初始化计数器

        using (file)
        {
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line = line.Replace("\x01", "|");
                if (IsJnetConfirmed(line))
                {
                    var order = ParseLine(line);
                    if (order != null)
                    {
                        orders[order.ClOrderId] = order;
                        count++;  // 只有成功解析的订单才计数
                    }
                    else
                    {
                        Console.WriteLine("Parse error: Unable to parse the line.");
                    }
                }
            }
        }

        Console.WriteLine($"JNET Confirmed Order Count: {count}");
        return orders;
    }


    public static Exception ExportToJsonl(Dictionary<string, JnetConfirmedOrder> orders, string jsonlFilename)
    {
        try
        {
            using (var file = new StreamWriter(jsonlFilename))
            {
                foreach (var order in orders.Values)
                {
                    string json = JsonConvert.SerializeObject(order);
                    file.WriteLine(json); // 写入JSON数据和换行符
                }
            }
        }
        catch (Exception ex)
        {
            return ex; // 返回异常
        }

        return null; // 无异常返回null
    }

    public static void FillSendTime(Dictionary<string, JnetConfirmedOrder> orders, string filename)
    {
        try
        {
            using (StreamReader file = new StreamReader(filename))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Replace("\x01", "|");

                    // Process for Client Reception Time
                    if (line.Contains("|35=D|") && line.Contains("8=FIX"))
                    {
                        UpdateOrderField(orders, line, reClOrderId, reTime, (order, match) => order.RecvClientTime = match);
                    }

                    // Process for Send Match Time
                    if (line.Contains("|35=D|") && line.Contains("|49=router_branch|") && line.Contains("|56=exch_sim|"))
                    {
                        UpdateOrderField(orders, line, reMatchOrderID, reTime, (order, match) => order.SendMatchTime = match);
                    }

                    // Process for Receive Match Time
                    if (line.Contains("|150=G|") && line.Contains("|49=exch_sim|") && line.Contains("|56=router_branch|"))
                    {
                        UpdateOrderField(orders, line, reMatchOrderID, reTime, (order, match) => order.RecvMatchTime = match);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error processing file: {ex.Message}");
        }
    }

    private static void UpdateOrderField(Dictionary<string, JnetConfirmedOrder> orders, string line, Regex orderIdRegex, Regex timeRegex, Action<JnetConfirmedOrder, string> updateField)
    {
        var orderIdMatches = orderIdRegex.Match(line);
        var timeMatches = timeRegex.Match(line);
        if (orderIdMatches.Success && timeMatches.Success && orders.TryGetValue(orderIdMatches.Groups[1].Value, out var order))
        {
            updateField(order, timeMatches.Groups[1].Value);
        }
    }

    private static string dateFormat = "MM/dd/yyyy HH:mm:ss.ffffff"; // C# date format, month and day swapped compared to Go

    public static void FillCostTime(Dictionary<string, JnetConfirmedOrder> orders)
    {
        foreach (var key in orders.Keys)
        {
            var order = orders[key];
            try
            {
                DateTime recvClientTime = DateTime.ParseExact(order.RecvClientTime, dateFormat, CultureInfo.InvariantCulture);
                DateTime sendMatchTime = DateTime.ParseExact(order.SendMatchTime, dateFormat, CultureInfo.InvariantCulture);
                DateTime recvMatchTime = DateTime.ParseExact(order.RecvMatchTime, dateFormat, CultureInfo.InvariantCulture);
                DateTime finalReturnTime = DateTime.ParseExact(order.FinalReturnTime, dateFormat, CultureInfo.InvariantCulture);

                TimeSpan omsCostTime1 = sendMatchTime - recvClientTime;
                TimeSpan matchCostTime = recvMatchTime - sendMatchTime;
                TimeSpan omsCostTime2 = finalReturnTime - recvMatchTime;
                TimeSpan totalCostTime = finalReturnTime - recvClientTime;

                order.OmsCostTime1 = omsCostTime1.TotalSeconds.ToString("F6");
                order.MatchCostTime = matchCostTime.TotalSeconds.ToString("F6");
                order.OmsCostTime2 = omsCostTime2.TotalSeconds.ToString("F6");
                order.TotalCostTime = totalCostTime.TotalSeconds.ToString("F6");

                orders[key] = order; // update the order in dictionary if needed
            }
            catch (FormatException ex)
            {
                throw new ApplicationException($"Error parsing date for order {order.ClOrderId}: {ex.Message}");
            }
        }
    }

    public static void ExportCsv(Dictionary<string, JnetConfirmedOrder> orders, string csvFilename)
    {
        try
        {
            using (var writer = new StreamWriter(csvFilename))
            {
                // Write header
                writer.WriteLine("Account,ClientOrderID,OmsCostTime1,MatchCostTime,OmsCostTime2,TotalCostTime");

                // Sort orders by RecvClientTime
                var sortedOrders = orders.Values.OrderBy(order => order.RecvClientTime).ToList();

                // Write records
                foreach (var order in sortedOrders)
                {
                    var record = $"{order.Account},{order.ClOrderId}," +
                                 $"{FormatTime(order.OmsCostTime1)},{FormatTime(order.MatchCostTime)}," +
                                 $"{FormatTime(order.OmsCostTime2)},{FormatTime(order.TotalCostTime)}";

                    writer.WriteLine(record);
                }
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error writing to CSV file: {ex.Message}");
        }
    }

    private static string FormatTime(string timeInSeconds)
    {
        if (double.TryParse(timeInSeconds, out double seconds))
        {
            // Convert seconds to milliseconds and format as string
            return (seconds * 1000).ToString("F3", CultureInfo.InvariantCulture);
        }
        else
        {
            throw new FormatException("Invalid time format.");
        }
    }

    static void Main(string[] args)
    {
        // if (args.Length < 3)
        // {
        //     Console.WriteLine("Usage: <program> <logFilePath> <outputCsvPath> \nVersion: 0.0.1");
        //     return;
        // }

        var logFilePath = "/home/jicheng.tang/work/cs1/a1/oms_20240508.log";
        var outputCsvPath = "./1.csv";

        try
        {
            var orders = GetAllJnetConfirmedOrders(logFilePath);
            FillSendTime(orders, logFilePath);
            FillCostTime(orders);
            ExportCsv(orders, outputCsvPath);
            ExportToJsonl(orders, "./1.jsonl");

            Console.WriteLine($"Orders exported successfully to {outputCsvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing orders: {ex.Message}");
        }
    }
}
