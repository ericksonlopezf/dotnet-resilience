```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v3


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 428.0 ns | 0.61 ns | 0.51 ns |  0.64 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 499.6 ns | 1.90 ns | 1.68 ns |  0.75 | 0.0095 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 665.8 ns | 0.74 ns | 0.62 ns |  1.00 | 0.0095 |     168 B |        1.00 |
