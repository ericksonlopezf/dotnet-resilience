
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v3


 Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 PermittedExecution | .NET 10.0 | .NET 10.0 | 372.5 ns | 0.66 ns | 0.58 ns |  0.74 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 9.0  | .NET 9.0  | 396.2 ns | 0.81 ns | 0.72 ns |  0.79 | 0.0110 |     184 B |        1.00 |
 PermittedExecution | .NET 8.0  | .NET 8.0  | 502.0 ns | 1.09 ns | 0.85 ns |  1.00 | 0.0105 |     184 B |        1.00 |
