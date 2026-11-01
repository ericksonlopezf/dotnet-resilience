```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


```
| Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 151.4 ns | 1.09 ns | 0.85 ns |  0.92 |    0.01 |      - |         - |          NA |
| DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 153.7 ns | 1.62 ns | 1.36 ns |  0.94 |    0.02 |      - |         - |          NA |
| DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 163.9 ns | 2.58 ns | 2.41 ns |  1.00 |    0.02 |      - |         - |          NA |
| EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 207.2 ns | 2.98 ns | 2.33 ns |  1.26 |    0.02 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 236.1 ns | 1.12 ns | 1.04 ns |  1.44 |    0.02 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 260.2 ns | 1.43 ns | 1.20 ns |  1.59 |    0.02 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 272.0 ns | 3.87 ns | 3.24 ns |  1.66 |    0.03 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 291.7 ns | 1.40 ns | 1.16 ns |  1.78 |    0.03 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 298.9 ns | 1.03 ns | 0.86 ns |  1.82 |    0.03 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 330.4 ns | 3.49 ns | 3.10 ns |  2.02 |    0.03 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 355.7 ns | 1.81 ns | 1.61 ns |  2.17 |    0.03 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 384.2 ns | 3.27 ns | 2.55 ns |  2.35 |    0.04 | 0.0086 |     144 B |          NA |
