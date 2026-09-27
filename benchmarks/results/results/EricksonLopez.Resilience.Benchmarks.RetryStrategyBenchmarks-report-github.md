```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v4


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 309.5 ns | 0.22 ns | 0.18 ns |  0.61 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 373.3 ns | 0.47 ns | 0.44 ns |  0.73 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 509.7 ns | 0.66 ns | 0.59 ns |  1.00 | 0.0095 |     168 B |        1.00 |
