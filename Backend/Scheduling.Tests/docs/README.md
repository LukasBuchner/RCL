# Scheduling.Tests

Unit and performance tests validating the scheduling algorithms using xUnit and Moq.

## Test Coverage

| Area                  | What is tested                                                                     |
|-----------------------|------------------------------------------------------------------------------------|
| **SCC Decomposition** | Strongly connected component detection, cycle identification, topological ordering |
| **LP Scheduling**     | Constraint generation, feasibility, optimal makespan calculation via OR-Tools      |
| **ConstrainedGroup**  | SS/FF coupled task handling, group merging, duration propagation                   |
| **Benchmarks**        | SCC vs no-SCC scheduling performance comparison (BenchmarkDotNet)                  |

## Running Tests

```bash
# From the Backend directory
dotnet test Scheduling.Tests/Scheduling.Tests.csproj

# With detailed output
dotnet test Scheduling.Tests/Scheduling.Tests.csproj --logger "console;verbosity=detailed"
```

## Performance Benchmarks

The `Performance/` directory contains BenchmarkDotNet benchmarks comparing scheduling strategies:

```bash
cd Scheduling.Benchmarks
dotnet run -c Release
```

## Related

- [Scheduling Module](../../Scheduling/docs/README.md) — the production scheduling implementation under test
- [Sunstone](../../../Sunstone/README.md) — Lean 4 formal verification of LP scheduling correctness
