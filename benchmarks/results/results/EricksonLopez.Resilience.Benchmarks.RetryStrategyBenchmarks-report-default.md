
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
 SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 426.8 ns | 1.77 ns | 1.48 ns |  0.67 | 0.0100 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 502.8 ns | 5.97 ns | 5.30 ns |  0.79 | 0.0095 |     168 B |        1.00 |
 SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 633.5 ns | 6.47 ns | 6.05 ns |  1.00 | 0.0095 |     168 B |        1.00 |
