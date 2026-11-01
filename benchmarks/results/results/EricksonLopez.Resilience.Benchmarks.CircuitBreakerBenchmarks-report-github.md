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
| ClosedCircuitHealthyExecution | .NET 10.0 | .NET 10.0 | 246.7 ns | 3.24 ns | 3.03 ns |  0.69 |    0.01 | 0.0100 |     168 B |        1.00 |
| ClosedCircuitHealthyExecution | .NET 9.0  | .NET 9.0  | 280.0 ns | 5.38 ns | 4.49 ns |  0.78 |    0.02 | 0.0100 |     168 B |        1.00 |
| ClosedCircuitHealthyExecution | .NET 8.0  | .NET 8.0  | 358.4 ns | 5.46 ns | 4.56 ns |  1.00 |    0.02 | 0.0100 |     168 B |        1.00 |
