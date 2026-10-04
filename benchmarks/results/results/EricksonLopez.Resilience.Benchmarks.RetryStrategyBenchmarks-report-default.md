
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 418.5 ns | 1.73 ns | 1.62 ns |  0.64 | 0.0100 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 505.1 ns | 1.38 ns | 1.29 ns |  0.77 | 0.0095 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 653.0 ns | 0.75 ns | 0.66 ns |  1.00 | 0.0095 |     168 B |        1.00 |
