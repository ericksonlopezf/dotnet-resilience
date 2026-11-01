```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 225.1 ns | 1.86 ns | 1.65 ns |  0.67 |    0.01 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 268.8 ns | 5.04 ns | 4.72 ns |  0.80 |    0.02 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 336.0 ns | 6.09 ns | 5.40 ns |  1.00 |    0.02 | 0.0100 |     168 B |        1.00 |
