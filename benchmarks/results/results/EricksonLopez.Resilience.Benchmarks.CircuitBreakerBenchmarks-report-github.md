```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.20 (9.0.20, 9.0.2026.41315), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.31 (8.0.31, 8.0.3126.42015), X64 RyuJIT x86-64-v3


```
| Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| ClosedCircuitHealthyExecution | .NET 10.0 | .NET 10.0 | 444.5 ns | 0.96 ns | 0.85 ns |  0.63 | 0.0100 |     168 B |        1.00 |
| ClosedCircuitHealthyExecution | .NET 9.0  | .NET 9.0  | 516.8 ns | 2.24 ns | 2.09 ns |  0.73 | 0.0095 |     168 B |        1.00 |
| ClosedCircuitHealthyExecution | .NET 8.0  | .NET 8.0  | 703.7 ns | 1.91 ns | 1.78 ns |  1.00 | 0.0095 |     168 B |        1.00 |
