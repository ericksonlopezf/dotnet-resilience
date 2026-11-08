```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| SuccessfulPathWithRetryPolicy | .NET 10.0 | .NET 10.0 | 219.3 ns | 1.80 ns | 1.69 ns |  0.70 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 9.0  | .NET 9.0  | 249.5 ns | 2.57 ns | 2.40 ns |  0.80 | 0.0100 |     168 B |        1.00 |
| SuccessfulPathWithRetryPolicy | .NET 8.0  | .NET 8.0  | 313.7 ns | 3.22 ns | 3.01 ns |  1.00 | 0.0100 |     168 B |        1.00 |
