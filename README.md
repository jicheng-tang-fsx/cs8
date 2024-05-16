# CSharp vs. Go

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
➜  publish git:(master) diff /home/jicheng.tang/work/v8/1.csv ./1.csv
➜  publish git:(master)
➜  publish git:(master)
```

| * | Go | CSharp |
|:---------|:---------|:---------|
| Total Cost (s) | 28.217 s | 18.960 s |



Go: https://github.com/jicheng-tang-fsx/v8.git 