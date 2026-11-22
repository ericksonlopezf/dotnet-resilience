
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 429.7 ns | 1.06 ns | 0.99 ns |  0.68 | 0.0100 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 492.7 ns | 4.81 ns | 4.50 ns |  0.77 | 0.0095 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 636.4 ns | 1.59 ns | 1.41 ns |  1.00 | 0.0095 |     168 B |        1.00 |
