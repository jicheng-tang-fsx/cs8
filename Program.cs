using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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


public class Program
{
    private static Regex reTime = new Regex(@"^D\d{4} (\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}\.\d{6})");
    private static Regex reClOrderId = new Regex(@"\|11=([^|]+)\|");
    private static Regex reAccount = new Regex(@"\|1=([^|]+)\|");

    public struct JnetConfirmedOrder
    {
        public string ClOrderId;
        public string Account;
        public string FinalReturnTime;
    }

    public static bool IsJnetConfirmed(string line)
    {
        return line.Contains("|35=D|");
    }

    public static JnetConfirmedOrder? ParseLine(string line)
    {
        var returnTimeMatches = reTime.Match(line);
        var clOrderIdMatches = reClOrderId.Match(line);
        var accountMatches = reAccount.Match(line);

        if (!returnTimeMatches.Success || !clOrderIdMatches.Success || !accountMatches.Success)
        {
            return null;
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

        using (var file = new StreamReader(filename))
        {
            string? line;  // Changed line to be explicitly nullable
            while ((line = file.ReadLine()) != null)
            {
                line = line.Replace("\x01", "|");
                if (IsJnetConfirmed(line))
                {
                    var order = ParseLine(line);
                    if (order.HasValue)
                    {
                        orders[order.Value.ClOrderId] = order.Value;
                    }
                }
            }
        }

        return orders;
    }

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: <program> <logFilePath>");
            return;
        }

        var logFilePath = args[0];
        try
        {
            var orders = GetAllJnetConfirmedOrders(logFilePath);
            Console.WriteLine($"JNET Confirmed Order Count: {orders.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file: {ex.Message}");
        }
    }
}
