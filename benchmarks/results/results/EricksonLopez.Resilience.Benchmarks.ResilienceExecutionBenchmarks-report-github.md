```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v4


```
| Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------- |---------- |---------:|--------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
| DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 169.1 ns | 0.34 ns | 0.28 ns | 169.0 ns |  0.78 |    0.03 |      - |         - |          NA |
| DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 198.3 ns | 0.13 ns | 0.10 ns | 198.2 ns |  0.92 |    0.04 |      - |         - |          NA |
| DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 216.5 ns | 4.31 ns | 8.42 ns | 221.1 ns |  1.00 |    0.05 |      - |         - |          NA |
| EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 267.1 ns | 0.36 ns | 0.30 ns | 267.0 ns |  1.24 |    0.05 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 327.4 ns | 0.39 ns | 0.34 ns | 327.4 ns |  1.51 |    0.06 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 373.8 ns | 0.85 ns | 0.76 ns | 373.9 ns |  1.73 |    0.07 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 374.4 ns | 0.94 ns | 0.78 ns | 374.7 ns |  1.73 |    0.07 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 382.6 ns | 0.37 ns | 0.33 ns | 382.6 ns |  1.77 |    0.07 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 434.4 ns | 0.32 ns | 0.30 ns | 434.4 ns |  2.01 |    0.08 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 463.1 ns | 0.45 ns | 0.38 ns | 463.0 ns |  2.14 |    0.08 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 465.3 ns | 0.51 ns | 0.46 ns | 465.4 ns |  2.15 |    0.08 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 517.0 ns | 3.08 ns | 2.73 ns | 517.2 ns |  2.39 |    0.09 | 0.0086 |     144 B |          NA |
