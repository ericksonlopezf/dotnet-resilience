```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.23 (9.0.23, 9.0.2326.52216), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.34 (8.0.34, 8.0.3426.52217), X64 RyuJIT x86-64-v3


```
| Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| PermittedExecution | .NET 10.0 | .NET 10.0 | 350.5 ns | 2.31 ns | 2.16 ns |  0.76 | 0.0110 |     184 B |        1.00 |
| PermittedExecution | .NET 9.0  | .NET 9.0  | 409.9 ns | 3.33 ns | 3.12 ns |  0.89 | 0.0110 |     184 B |        1.00 |
| PermittedExecution | .NET 8.0  | .NET 8.0  | 458.6 ns | 2.10 ns | 1.75 ns |  1.00 | 0.0110 |     184 B |        1.00 |
