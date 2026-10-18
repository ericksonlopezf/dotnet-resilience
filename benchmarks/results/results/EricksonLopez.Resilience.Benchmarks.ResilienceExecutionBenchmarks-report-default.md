
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v3


 Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 232.5 ns | 0.38 ns | 0.34 ns |  0.91 |      - |         - |          NA |
 DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 242.0 ns | 0.59 ns | 0.52 ns |  0.95 |      - |         - |          NA |
 DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 255.7 ns | 0.36 ns | 0.34 ns |  1.00 |      - |         - |          NA |
 EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 413.8 ns | 0.47 ns | 0.39 ns |  1.62 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 427.6 ns | 2.08 ns | 1.84 ns |  1.67 | 0.0086 |     144 B |          NA |
 EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 496.6 ns | 0.80 ns | 0.71 ns |  1.94 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 512.3 ns | 0.87 ns | 0.77 ns |  2.00 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 537.1 ns | 1.22 ns | 1.09 ns |  2.10 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 537.4 ns | 2.33 ns | 2.06 ns |  2.10 | 0.0086 |     144 B |          NA |
 ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 598.0 ns | 1.11 ns | 0.99 ns |  2.34 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 610.0 ns | 0.99 ns | 0.87 ns |  2.39 | 0.0086 |     144 B |          NA |
 EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 685.7 ns | 1.57 ns | 1.31 ns |  2.68 | 0.0086 |     144 B |          NA |
