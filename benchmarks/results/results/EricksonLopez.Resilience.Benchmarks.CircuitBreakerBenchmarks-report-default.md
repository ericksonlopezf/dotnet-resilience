
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 ClosedCircuitHealthyExecution | .NET 10.0 | .NET 10.0 | 455.9 ns | 1.57 ns | 1.39 ns |  0.66 | 0.0100 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 9.0  | .NET 9.0  | 518.8 ns | 1.97 ns | 1.65 ns |  0.75 | 0.0095 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 8.0  | .NET 8.0  | 691.7 ns | 3.30 ns | 2.92 ns |  1.00 | 0.0095 |     168 B |        1.00 |
