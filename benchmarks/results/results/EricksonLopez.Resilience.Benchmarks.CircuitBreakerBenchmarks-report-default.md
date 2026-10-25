
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 10.0 : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  .NET 9.0  : .NET 9.0.21 (9.0.21, 9.0.2126.50404), X64 RyuJIT x86-64-v4
  .NET 8.0  : .NET 8.0.32 (8.0.32, 8.0.3226.50405), X64 RyuJIT x86-64-v4


 Method                        | Job       | Runtime   | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
------------------------------ |---------- |---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
 ClosedCircuitHealthyExecution | .NET 10.0 | .NET 10.0 | 331.8 ns | 4.27 ns | 4.00 ns |  0.61 |    0.01 | 0.0019 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 9.0  | .NET 9.0  | 422.0 ns | 4.71 ns | 4.40 ns |  0.78 |    0.01 | 0.0019 |     168 B |        1.00 |
 ClosedCircuitHealthyExecution | .NET 8.0  | .NET 8.0  | 541.5 ns | 9.12 ns | 8.53 ns |  1.00 |    0.02 | 0.0019 |     168 B |        1.00 |
