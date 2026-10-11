
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


 Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 PermittedExecution | .NET 10.0 | .NET 10.0 | 362.6 ns | 0.96 ns | 0.80 ns |  0.79 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 9.0  | .NET 9.0  | 395.0 ns | 0.92 ns | 0.82 ns |  0.86 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 8.0  | .NET 8.0  | 460.1 ns | 2.41 ns | 2.14 ns |  1.00 | 0.0110 |     184 B |        1.00 |
