# Agent Serialization — Client UX

> How React and Kotlin render violations, dispatch execution errors, and present fix suggestions.

## Overview

Both clients consume the same `ProcedureValidationChanged` GraphQL subscription for soft warnings, and the same `AGENT_SERIALIZATION_VIOLATION` GraphQL error code for hard-gate rejections. The two paths converge on one visual treatment per client: a warning badge on affected nodes, a persistent banner above the canvas, and a detail modal with three fix suggestions.

## Key Concepts

- **Soft warning** — derived from the subscription; updates as the procedure is edited.
- **Hard rejection** — thrown by the backend when `startProcedure` is called with outstanding violations; carries the same structured data.
- **Error-code registry** — a per-client table that maps GraphQL error codes to UX actions, so new typed execution errors do not touch the start-procedure call site.

## React

### Soft warning subscription

```typescript
useSubscription(OnProcedureValidationChangedDocument, {
  skip: !loadedProcedure,
  onData: ({ data: { data } }) => {
    if (data?.procedureValidationChanged) {
      setValidationResult(data.procedureValidationChanged);
    }
  },
});
```

Pattern mirrors `OnNodesChangedDocument` and `OnEdgesChangedDocument`. No client-side debounce or diffing — the server pushes only when violations actually change.

### Node warning variant

`motionVariants.ts` defines a `hasWarning` Framer Motion variant using `var(--bs-warning)` and a soft box shadow. `BaseNode.tsx` applies it alongside the existing `executing`, `selected`, `markedForCut`, and `markedForCopy` variants. A warning icon badge is overlaid in the top-right corner so the signal is not colour-only.

### Execution error handler registry

`useExecutionErrorHandlers` maps GraphQL error codes to UX actions. Adding a new typed execution error adds an entry; the play-button handler is unchanged.

```typescript
const useExecutionErrorHandlers = ({ setViolations, setShowModal, handleError }) =>
  useMemo(() => ({
    AGENT_SERIALIZATION_VIOLATION: (error) => {
      const violations = (error.extensions?.data as AgentSerializationViolation[]) ?? [];
      setViolations(violations);
      setShowModal(true);
    },
    EXECUTION_ALREADY_IN_PROGRESS: () => {
      handleError(new Error("A procedure is already running"), { source: "graphql" });
    },
  }), [setViolations, setShowModal, handleError]);
```

`onPlayClick` performs a Layer 1 subscription check, then calls `startProcedure()`. GraphQL errors dispatch through the registry; unrecognised errors fall through to the generic toast.

### UX per error code

| Error code | Component | Behaviour |
|---|---|---|
| `AGENT_SERIALIZATION_VIOLATION` | `AgentSerializationModal` | Modal with per-agent violation details and three fix suggestions |
| `EXECUTION_ALREADY_IN_PROGRESS` | `ErrorContext` → `ErrorToast` | Warning toast: "A procedure is already running" |
| Unrecognised / generic | `ErrorContext` → `ErrorToast` | Error toast with server message |
| Non-GraphQL (network) | `ErrorContext` → `ErrorToast` | Error toast with connection message |

## Kotlin (Material 3 Expressive)

### Subscription

```kotlin
apolloClient.subscription(OnProcedureValidationChangedSubscription())
    .toFlow()
    .collect { response ->
        _validationResult.value = response.data?.procedureValidationChanged
    }
```

`_validationResult` is a `StateFlow<ProcedureValidationResult?>` on `FlowRepository`, consumed by `FlowScreen` as Compose state.

### Node warning state

`FlowNodeCard` borders use `colorScheme.error` when `hasWarning` is true, at the same 2 dp width and elevation as the executing state. The value animates via `animateColorAsState` with `MaterialTheme.motionScheme.fastEffectsSpec()`. A `WarningBadge` — `Icons.Outlined.Warning` inside a `CircleShape` backed by `errorContainer` — is positioned at the top-right with `semantics { contentDescription = "Warning: agent serialization issue" }` for screen readers. `FlowScreen` passes `hasWarning = node.id in warningNodeIds`, where `warningNodeIds` is derived from `validationResult.agentSerializationViolations`.

### Validation warning banner

Between the timeline and the canvas, a persistent `Surface(color = errorContainer)` shows `"$totalSkills skills could run at the same time"` with a "Details" action opening the dialog.

### Execution error mapper

Kotlin mirrors the React registry as a `when` over the GraphQL `extensions.code`:

```kotlin
fun mapExecutionErrors(errors: List<Error>): StartProcedureResult? {
    for (error in errors) {
        when (error.extensions?.get("code") as? String) {
            "AGENT_SERIALIZATION_VIOLATION" ->
                return StartProcedureResult.ValidationFailed(parseViolationsFromExtensions(error.extensions))
            "EXECUTION_ALREADY_IN_PROGRESS" ->
                return StartProcedureResult.AlreadyRunning
        }
    }
    return null
}
```

`StartProcedureResult` is a sealed hierarchy of `Success`, `ValidationFailed`, `AlreadyRunning`, and `Error`. The play-button handler in `FlowScreen` uses an exhaustive `when` so new result variants fail the build until they are rendered.

### UX per result

| Result | Component | Behaviour |
|---|---|---|
| `ValidationFailed` | `AgentSerializationDialog` | AlertDialog with violation cards and fix suggestions |
| `AlreadyRunning` | M3 `Snackbar` | Short snackbar: "A procedure is already running" |
| `Error` | M3 `Snackbar` with dismiss | Long snackbar with server message |
| `Success` | none | `ExecutionIndicator` takes over |

## Cross-client parity

| Surface | React | Kotlin |
|---|---|---|
| Node warning colour | `var(--bs-warning)` amber + shadow | `colorScheme.error` + 2 dp border |
| Warning badge | SVG icon overlay | `Icons.Outlined.Warning` in `errorContainer` circle |
| Banner | Top-of-canvas banner via `ErrorContext` | `Surface` with `errorContainer` between timeline and canvas |
| Detail view | Modal with per-agent cards | `AlertDialog` with `LazyColumn` of `ViolationCard`s |
| Fix suggestions | Three numbered options | Same three options, M3 `bodyMedium` typography |
| Accessibility | `aria-label` on badge | `semantics { contentDescription }` on badge |
| Motion | Framer Motion `hasWarning` variant | `animateColorAsState` + `fastEffectsSpec()` |

## UX guidelines

### Three fix suggestions

1. Add a finish-to-start connection to define execution order between the skills.
2. Assign one of the skills to a different agent.
3. Move the skills into different router branches (only one branch runs per execution).

### Grouping by agent

With $K$ unserialised same-agent skills, the violation record is one per agent listing all $K$ skills — not $K(K-1)/2$ pair-level entries. Both clients surface this shape directly.

### Wording

Avoid jargon. The modal leads with "Skills could run at the same time"; the detail per agent reads "`Agent 'X'` has N skills with no execution order", followed by the skill list and the three fix suggestions.

### Accessibility

Warning state is never colour-only: badge icon, screen-reader text, and high-contrast support are mandatory on both clients.

## Related Documentation

- [Agent Serialization overview](README.md)
- [Validator](validator.md) — the GraphQL error and subscription this UX consumes
- [Proofs](proofs.md) — the soundness result behind the hard gate
- [Verification](verification.md) — how to exercise the end-to-end flow
- [GraphQL Operations](../../GraphQLServer/docs/graphql-operations.md)
