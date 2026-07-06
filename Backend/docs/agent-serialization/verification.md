# Agent Serialization — Verification

> Commands and test suites that exercise the proofs, the validator, the procedure-scope guard, and the full hard-gate path.

## Overview

Four layers carry the invariant: the Lean proof, the validator unit tests, the procedure-scope guard tests, and the integration suite. Each has a reproducer command and a clear pass criterion.

## Formal proofs

```bash
cd Sunstone
lake build Sunstone.Scheduling.AgentSerialization
```

The build is `sorry`-free for every theorem named in [Proofs](proofs.md) — `fs_reachable_prevents_overlap`, `anyPath_start_monotone`, `fsThenAny_prevents_overlap`, `agent_serialization_sound`, `agent_serialization_sound_fs_only`, and the supporting lemmas.

## Validator unit tests

```bash
cd Backend
dotnet test Application.Tests/Application.Tests.csproj \
  --filter "FullyQualifiedName~AgentSerializationValidatorTests"
```

### Scenario matrix

| # | Scenario | Setup | Expected |
|---|----------|-------|----------|
| 1 | Single agent, single skill | 1 skill node | No violations |
| 2 | Different agents, no FS | 2 skills, different `AgentId`s | No violations |
| 3 | Direct FS edge | `A →FS→ B`, same agent | No violations |
| 4 | Transitive FS chain | `A →FS→ M →FS→ B`, same agent on A and B | No violations |
| 5 | Same agent, no edge | 2 skills, same agent, no edges | 1 violation, 1 pair |
| 6 | Same agent, SS only | `A →SS→ B`, same agent | 1 violation |
| 7 | Same agent, FF only | `A →FF→ B`, same agent | 1 violation |
| 8 | Same agent, SF only | `A →SF→ B`, same agent | 1 violation |
| 9 | Mutually exclusive branches | 2 skills, same agent, different router branches | No violations |
| 10 | Same branch, no FS | 2 skills, same agent, same router branch, no FS | 1 violation |
| 11 | Three skills, full chain | `A →FS→ B →FS→ C`, all same agent | No violations |
| 12 | Three skills, broken chain | `A →FS→ B`, C unconnected, all same agent | 1 violation, 2 pairs (A–C, B–C) |
| 13 | FS through `TaskNode` | `A →FS→ TaskNode →FS→ B` | No violations |
| 14 | Nested routers, outer-exclusive | 2 skills in different outer branches | No violations |
| 15 | Nested routers, inner-exclusive | 2 skills in different inner branches, same outer branch | No violations |
| 16 | Mixed valid/invalid | Agent X: `A →FS→ B` (valid); Agent Y: `C`, `D` unconnected | 1 violation (agent Y only) |
| 17 | Empty procedure | No nodes | No violations |
| 18 | `TaskNode`s only | No skill nodes | No violations |
| L2-a | Unsafe FS+SS+FS | `A1 →FS→ B1`, `B1 →SS→ A2`, `B1 →FS→ A3`, A1/A2/A3 same agent | Violations on (A1, A2) |
| L2-b | Fix via FS between pair | L2-a plus `A2 →FS→ A3` | Corresponding pair is no longer flagged |
| L2-c | Direct SS on same-agent pair | `A →SS→ B`, same agent | Violation |
| L2-d | `FS → SS` accepted | `A →FS→ M →SS→ B`, same agent on A and B | No violations |
| L2-e | `SS → FS` rejected | `A →SS→ M →FS→ B`, same agent on A and B | Violation |

### `ProcedureValidationTracker` reactive tests

| # | Scenario | Setup | Expected |
|---|----------|-------|----------|
| 19 | Emit on violation change | Push nodes with a same-agent conflict, then push an FS edge resolving it | Two emissions: first with violation, second empty |
| 20 | Suppress on no change | Push a position-only node update | No new emission (`DistinctUntilChanged`) |
| 21 | Throttle coalesces bursts | Push 5 node updates within 500 ms | At most one emission after the throttle window |

## Procedure-scope guard tests

```bash
dotnet test Application.Tests/Application.Tests.csproj \
  --filter "FullyQualifiedName~ProcedureStateTrackerCrossProcedureGuardTests"
```

Ten scenarios cover the `UpdateEntities` trust boundary:

- Mixed-procedure payloads are filtered to the loaded procedure with a `LogCrossProcedureEntitiesDropped` warning.
- Updates with no procedure loaded are rejected with `LogUpdateRejectedNoProcedure`.
- All-matching payloads pass through unchanged.
- Empty payloads stay silent.

## Integration

```bash
dotnet test Application.Tests/Application.Tests.csproj
```

The full suite covers the hard-gate path — `ExecutionOrchestrator.StartLoadedProcedureAsync` throws `AgentSerializationException`, `Mutation.cs` catches it, and the GraphQL response carries error code `AGENT_SERIALIZATION_VIOLATION` with structured `extensions.data` — and the soft-subscription path via `ProcedureValidationTracker`.

## Formatting and warnings

```bash
dotnet build Freydis.sln                                          # zero warnings
dotnet format Freydis.sln --verify-no-changes --verbosity minimal
```

## Client checks

- **React**: `useExecutionErrorHandlers` subscribes to `ProcedureValidationChanged` for Layer 1 soft warnings and dispatches `AGENT_SERIALIZATION_VIOLATION` to the modal for the Layer 3 hard fallback. `BaseNode.tsx` renders warning badges on affected nodes.
- **Kotlin**: `ExecutionErrorMapper.parseViolationsFromExtensions` turns the GraphQL `extensions.data` into `StartProcedureResult.ValidationFailed`; `FlowScreen.kt` renders via an exhaustive `when` over the sealed hierarchy.

## Related Documentation

- [Agent Serialization overview](README.md)
- [Proofs](proofs.md)
- [Validator](validator.md)
- [Client UX](client-ux.md)
- [Lean ↔ C# Cross Reference](../../../Sunstone/docs/cross-reference.md)
- [Assumption Audit](../../../Sunstone/docs/assumption-audit.md)
