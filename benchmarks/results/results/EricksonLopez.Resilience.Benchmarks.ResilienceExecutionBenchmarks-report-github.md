```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


```
| Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 229.3 ns | 0.47 ns | 0.42 ns |  0.91 |      - |         - |          NA |
| DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 240.9 ns | 0.97 ns | 0.86 ns |  0.95 |      - |         - |          NA |
| DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 252.4 ns | 0.77 ns | 0.72 ns |  1.00 |      - |         - |          NA |
| EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 385.9 ns | 1.70 ns | 1.50 ns |  1.53 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 429.3 ns | 1.30 ns | 1.15 ns |  1.70 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 490.2 ns | 0.72 ns | 0.64 ns |  1.94 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 501.0 ns | 1.32 ns | 1.24 ns |  1.99 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 527.9 ns | 0.55 ns | 0.46 ns |  2.09 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 533.9 ns | 1.37 ns | 1.28 ns |  2.12 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 576.5 ns | 0.99 ns | 0.88 ns |  2.28 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 621.9 ns | 2.29 ns | 1.91 ns |  2.46 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 647.4 ns | 1.46 ns | 1.30 ns |  2.57 | 0.0086 |     144 B |          NA |
