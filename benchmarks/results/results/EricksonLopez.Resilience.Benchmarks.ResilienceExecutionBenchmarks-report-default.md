
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


 Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
 DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 229.2 ns | 0.41 ns | 0.39 ns |  0.83 |    0.00 |      - |         - |          NA |
 DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 230.8 ns | 0.76 ns | 0.67 ns |  0.83 |    0.00 |      - |         - |          NA |
 DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 276.4 ns | 1.23 ns | 1.02 ns |  1.00 |    0.01 |      - |         - |          NA |
 EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 398.5 ns | 3.10 ns | 2.75 ns |  1.44 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 426.5 ns | 0.83 ns | 0.69 ns |  1.54 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 483.3 ns | 6.94 ns | 6.15 ns |  1.75 |    0.02 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 495.5 ns | 3.44 ns | 3.22 ns |  1.79 |    0.01 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 524.7 ns | 6.06 ns | 5.06 ns |  1.90 |    0.02 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 527.5 ns | 4.37 ns | 3.65 ns |  1.91 |    0.01 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 579.8 ns | 2.43 ns | 2.03 ns |  2.10 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 616.2 ns | 1.16 ns | 0.97 ns |  2.23 |    0.01 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 663.6 ns | 3.72 ns | 3.30 ns |  2.40 |    0.01 | 0.0086 |     144 B |          NA |
