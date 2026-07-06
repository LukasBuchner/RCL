using BenchmarkDotNet.Running;
using FHOOE.Freydis.Scheduling.Benchmarks;

// SCC vs No-SCC comparison (scheduling algorithm only)
BenchmarkRunner.Run<SccVsNoSccBenchmarks>();

// Alternative: Run all benchmarks in this project
// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);