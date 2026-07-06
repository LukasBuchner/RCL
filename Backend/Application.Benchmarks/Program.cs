using BenchmarkDotNet.Running;
using FHOOE.Freydis.Application.Benchmarks;

// Discover every benchmark class in this assembly so each is selectable by filter, e.g.
//   dotnet run -c Release -- --filter '*ReactiveExecution*'
//   dotnet run -c Release -- --filter '*EndToEndPipeline*'
// EndToEndPipelineBenchmarks (scheduling latency) and ReactiveExecutionBenchmarks (dual-loop
// convergence) each carry their own [Config]; the SCC-vs-monolithic benchmark lives in the
// separate Scheduling.Benchmarks project.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);