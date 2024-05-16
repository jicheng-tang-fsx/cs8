# CSharp vs. Go

| * | Go | CSharp |
|:---------|:---------|:---------|
| Total Cost (s) | 28.217 s | 18.960 s |

```bash
# Go
➜  v8 git:(main) time ./v8
JNET Correction Order Count:  22426
Orders exported successfully to ./1.csv
./v8  29.24s user 4.01s system 117% cpu 28.217 total

# CSharp
➜  publish git:(master) time ./a1
JNET Confirmed Order Count: 22426
Orders exported successfully to ./1.csv
./a1  16.47s user 2.52s system 100% cpu 18.960 total
```

Same result
```bash
➜  publish git:(master) head ./1.csv
Account,ClientOrderID,OmsCostTime1,MatchCostTime,OmsCostTime2,TotalCostTime
RSIT_FDP_ACCOUNT_1,9002022171516557900000901262,1.620,9719.046,0.634,9721.300
RSIT_FDP_ACCOUNT_3,9002022171516557900000903760,10.241,9711.585,2.513,9724.339
RSIT_FDP_ACCOUNT_7,9002022171516557900000907878,9.317,9712.698,4.093,9726.108
RSIT_FDP_ACCOUNT_8,90020221715165579000009083,9.634,9712.542,4.044,9726.220
RSIT_FDP_ACCOUNT_1,900202217151655790000091019,2.085,9676.829,1.992,9680.906
RSIT_FDP_ACCOUNT_2,9002022171516557900000920390,1.869,9630.106,2.147,9634.122
RSIT_FDP_ACCOUNT_5,900202217151655790000092396,1.935,9630.374,4.358,9636.667
RSIT_FDP_ACCOUNT_6,9002022171516557900000924535,2.338,9629.694,5.474,9637.506
RSIT_FDP_ACCOUNT_1,9002022171516557900000928956,1.413,9586.468,5.607,9593.488
➜  publish git:(master) md5sum ./1.csv
4bee26ffe1714c0ab547c77d677ce825  ./1.csv
➜  publish git:(master) md5sum /home/jicheng.tang/work/v8/1.csv
4bee26ffe1714c0ab547c77d677ce825  /home/jicheng.tang/work/v8/1.csv
```

Go: https://github.com/jicheng-tang-fsx/v8.git 