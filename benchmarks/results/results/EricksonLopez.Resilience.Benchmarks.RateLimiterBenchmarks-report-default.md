
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


 Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 PermittedExecution | .NET 10.0 | .NET 10.0 | 196.5 ns | 1.48 ns | 1.31 ns |  0.80 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 9.0  | .NET 9.0  | 222.5 ns | 2.43 ns | 2.27 ns |  0.90 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 8.0  | .NET 8.0  | 246.1 ns | 2.72 ns | 2.54 ns |  1.00 | 0.0110 |     184 B |        1.00 |
