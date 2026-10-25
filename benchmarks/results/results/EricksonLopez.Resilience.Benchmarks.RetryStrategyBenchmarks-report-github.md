```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v4


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 316.3 ns | 3.47 ns | 3.24 ns |  0.64 |    0.01 | 0.0019 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 385.5 ns | 2.82 ns | 2.35 ns |  0.77 |    0.01 | 0.0019 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 497.6 ns | 8.61 ns | 8.06 ns |  1.00 |    0.02 | 0.0019 |     168 B |        1.00 |
