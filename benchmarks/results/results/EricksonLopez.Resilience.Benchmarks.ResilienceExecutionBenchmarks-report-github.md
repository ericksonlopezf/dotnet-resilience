```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v4


```
| Method                     | Job       | Runtime   | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------- |---------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 194.6 ns |  3.17 ns |  2.96 ns |  0.78 |    0.02 |      - |         - |          NA |
| DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 213.4 ns |  2.32 ns |  2.17 ns |  0.85 |    0.02 |      - |         - |          NA |
| DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 250.6 ns |  4.25 ns |  3.97 ns |  1.00 |    0.02 |      - |         - |          NA |
| EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 311.2 ns |  5.92 ns |  6.08 ns |  1.24 |    0.03 | 0.0014 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 356.0 ns |  7.01 ns |  7.50 ns |  1.42 |    0.04 | 0.0014 |     144 B |          NA |
| ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 398.7 ns |  5.19 ns |  4.60 ns |  1.59 |    0.03 | 0.0014 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 410.0 ns |  4.84 ns |  4.52 ns |  1.64 |    0.03 | 0.0014 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 426.5 ns |  5.72 ns |  5.35 ns |  1.70 |    0.03 | 0.0014 |     144 B |          NA |
| ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 458.3 ns |  8.57 ns |  9.17 ns |  1.83 |    0.05 | 0.0014 |     144 B |          NA |
| ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 486.6 ns |  6.83 ns |  5.71 ns |  1.94 |    0.04 | 0.0010 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 529.4 ns | 10.64 ns | 10.45 ns |  2.11 |    0.05 | 0.0010 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 646.7 ns | 12.24 ns | 12.02 ns |  2.58 |    0.06 | 0.0010 |     144 B |          NA |
