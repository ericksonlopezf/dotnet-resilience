```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v4


```
| Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| PermittedExecution | .NET 10.0 | .NET 10.0 | 277.3 ns | 2.35 ns | 1.96 ns |  0.73 |    0.01 | 0.0019 |     184 B |        1.00 |
| PermittedExecution | .NET 9.0  | .NET 9.0  | 349.7 ns | 1.95 ns | 1.63 ns |  0.92 |    0.01 | 0.0019 |     184 B |        1.00 |
| PermittedExecution | .NET 8.0  | .NET 8.0  | 379.0 ns | 6.08 ns | 5.69 ns |  1.00 |    0.02 | 0.0019 |     184 B |        1.00 |
