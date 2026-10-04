
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


 Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 232.9 ns | 0.21 ns | 0.19 ns |  0.88 |      - |         - |          NA |
 DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 242.5 ns | 0.52 ns | 0.46 ns |  0.92 |      - |         - |          NA |
 DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 265.0 ns | 0.75 ns | 0.70 ns |  1.00 |      - |         - |          NA |
 EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 391.0 ns | 2.46 ns | 2.30 ns |  1.48 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 407.6 ns | 1.93 ns | 1.80 ns |  1.54 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 493.3 ns | 1.91 ns | 1.60 ns |  1.86 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 495.3 ns | 0.78 ns | 0.73 ns |  1.87 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 530.7 ns | 1.14 ns | 1.01 ns |  2.00 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 552.4 ns | 0.98 ns | 0.92 ns |  2.08 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 558.9 ns | 1.79 ns | 1.58 ns |  2.11 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 620.7 ns | 1.50 ns | 1.33 ns |  2.34 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 664.0 ns | 1.73 ns | 1.44 ns |  2.51 | 0.0086 |     144 B |          NA |
