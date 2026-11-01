```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.22 (9.0.22, 9.0.2226.52218), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.33 (8.0.33, 8.0.3326.52219), X64 RyuJIT x86-64-v4


```
| Method             | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| PermittedExecution | .NET 10.0 | .NET 10.0 | 208.3 ns | 2.71 ns | 2.26 ns |  0.79 |    0.01 | 0.0110 |     184 B |        1.00 |
| PermittedExecution | .NET 9.0  | .NET 9.0  | 241.7 ns | 2.54 ns | 2.12 ns |  0.92 |    0.02 | 0.0110 |     184 B |        1.00 |
| PermittedExecution | .NET 8.0  | .NET 8.0  | 262.1 ns | 4.78 ns | 4.24 ns |  1.00 |    0.02 | 0.0110 |     184 B |        1.00 |
