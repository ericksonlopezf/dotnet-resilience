
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v4


 Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 PermittedExecution | .NET 10.0 | .NET 10.0 | 273.2 ns | 1.10 ns | 0.98 ns |  0.71 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 9.0  | .NET 9.0  | 322.4 ns | 0.75 ns | 0.63 ns |  0.83 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 8.0  | .NET 8.0  | 386.5 ns | 1.92 ns | 1.79 ns |  1.00 | 0.0110 |     184 B |        1.00 |
