
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


 Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
 DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 223.6 ns | 0.68 ns | 0.56 ns |  0.87 |    0.01 |      - |         - |          NA |
 DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 236.8 ns | 1.78 ns | 1.66 ns |  0.92 |    0.01 |      - |         - |          NA |
 DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 257.5 ns | 2.14 ns | 1.90 ns |  1.00 |    0.01 |      - |         - |          NA |
 EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 405.0 ns | 1.79 ns | 1.59 ns |  1.57 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 452.5 ns | 1.53 ns | 1.20 ns |  1.76 |    0.01 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 487.3 ns | 1.05 ns | 0.99 ns |  1.89 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 511.1 ns | 2.98 ns | 2.64 ns |  1.99 |    0.02 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 536.9 ns | 4.69 ns | 3.91 ns |  2.09 |    0.02 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 544.0 ns | 4.64 ns | 4.11 ns |  2.11 |    0.02 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 579.9 ns | 6.50 ns | 5.76 ns |  2.25 |    0.03 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 657.8 ns | 3.02 ns | 2.52 ns |  2.56 |    0.02 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 658.4 ns | 4.34 ns | 3.62 ns |  2.56 |    0.02 | 0.0086 |     144 B |          NA |
