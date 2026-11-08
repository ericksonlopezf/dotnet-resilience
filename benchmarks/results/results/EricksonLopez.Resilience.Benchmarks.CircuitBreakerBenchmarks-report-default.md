
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
 ClosedCircuitHealthyExecution | .NET 10.0 | .NET 10.0 | 228.2 ns | 2.29 ns | 2.14 ns |  0.65 |    0.01 | 0.0100 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 9.0  | .NET 9.0  | 276.5 ns | 3.54 ns | 3.31 ns |  0.79 |    0.01 | 0.0100 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 8.0  | .NET 8.0  | 348.6 ns | 4.80 ns | 4.49 ns |  1.00 |    0.02 | 0.0100 |     168 B |        1.00 |
