
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


 Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 PermittedExecution | .NET 10.0 | .NET 10.0 | 353.7 ns | 0.73 ns | 0.64 ns |  0.77 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 9.0  | .NET 9.0  | 412.2 ns | 1.68 ns | 1.49 ns |  0.90 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 8.0  | .NET 8.0  | 459.7 ns | 1.12 ns | 1.05 ns |  1.00 | 0.0110 |     184 B |        1.00 |
