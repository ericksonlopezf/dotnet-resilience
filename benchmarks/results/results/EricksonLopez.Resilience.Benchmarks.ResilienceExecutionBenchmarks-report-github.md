```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


```
| Method                     | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| DirectPollyExecution       | .NET 10.0 | .NET 10.0 | 137.5 ns | 1.84 ns | 1.72 ns |  0.93 |    0.01 |      - |         - |          NA |
| DirectPollyExecution       | .NET 9.0  | .NET 9.0  | 138.9 ns | 1.21 ns | 1.13 ns |  0.94 |    0.01 |      - |         - |          NA |
| DirectPollyExecution       | .NET 8.0  | .NET 8.0  | 147.7 ns | 1.21 ns | 1.13 ns |  1.00 |    0.01 |      - |         - |          NA |
| EcosystemPipelineExecution | .NET 10.0 | .NET 10.0 | 187.4 ns | 2.54 ns | 2.38 ns |  1.27 |    0.02 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 9.0  | .NET 9.0  | 226.4 ns | 1.93 ns | 1.71 ns |  1.53 |    0.02 | 0.0086 |     144 B |          NA |
| EcosystemPipelineExecution | .NET 8.0  | .NET 8.0  | 247.2 ns | 2.55 ns | 2.38 ns |  1.67 |    0.02 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 10.0 | .NET 10.0 | 259.5 ns | 2.27 ns | 2.13 ns |  1.76 |    0.02 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 10.0 | .NET 10.0 | 271.7 ns | 3.41 ns | 3.19 ns |  1.84 |    0.02 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 9.0  | .NET 9.0  | 287.4 ns | 4.33 ns | 4.05 ns |  1.95 |    0.03 | 0.0086 |     144 B |          NA |
| ResultResilientExecution   | .NET 8.0  | .NET 8.0  | 312.8 ns | 3.56 ns | 3.33 ns |  2.12 |    0.03 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 9.0  | .NET 9.0  | 342.3 ns | 4.41 ns | 4.13 ns |  2.32 |    0.03 | 0.0086 |     144 B |          NA |
| EcosystemExecutorExecution | .NET 8.0  | .NET 8.0  | 353.5 ns | 4.75 ns | 4.44 ns |  2.39 |    0.03 | 0.0086 |     144 B |          NA |
