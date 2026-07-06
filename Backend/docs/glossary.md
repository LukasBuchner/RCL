# Glossary

> Every domain term used in the Freydis backend, defined in plain English.

---

## Core Domain Terms

### Procedure

A workflow — a collection of tasks with dependencies that defines what robots should do and in what order. Think of it
as a recipe: "Robot A picks up the part, then Robot B welds it, then Robot A places it." Procedures are designed
visually in the frontend and stored in the database.

### Node

A single item in a procedure's visual graph. Every procedure is a collection of nodes connected by edges. There are
three types:

- **TaskNode** — A grouping container (like a folder) that holds other nodes
- **SkillExecutionNode** — An actual action performed by a robot (e.g., "pick up part")
- **RouterNode** — A decision point that evaluates a condition and picks one branch to execute

See: [Domain Layer](../Domain/docs/README.md)

### Task

The data attached to a node that describes *what* it does. Each node type has its own task type:

- **Task** — Basic timing info (name, duration, start time) for TaskNodes
- **SkillExecutionTask** — Links a skill to an agent, with timing info
- **RouterTask** — Contains a selector expression and a list of branches

### Skill

A named capability that an agent can perform — like "pick up", "weld", or "move to position". Skills have **properties
** (inputs/outputs) that can be bound to variables. Skills are defined at the system level and assigned to agents.

### Agent

A robot or simulation that can execute skills. The three kinds are distinct `IRuntimeAgent` implementations (not a
discriminator on the `Agent` domain record, which carries `AgentState` and `LastSeenUtc`):

- **Dummy** — Simulated agent for testing (just waits for the skill duration)
- **KUKA iiwa 14** — Real industrial robot, controlled via OPC UA
- **Digital Twin** — Unity-based simulation, connected via WebSocket

See: [Agents Module](../Agents/docs/README.md)

### Edge / DependencyEdge

A connection between two nodes that defines execution order. Edges have a **dependency type**:

- **FS (Finish-to-Start)** — Node B starts after Node A finishes (most common)
- **SS (Start-to-Start)** — Node B starts when Node A starts
- **SF (Start-to-Finish)** — Node B finishes when Node A starts
- **FF (Finish-to-Finish)** — Node B finishes when Node A finishes

### Router

A decision-making node. When execution reaches a router, it evaluates a condition (the **selector expression**) against
runtime **variables** and picks one **branch** to execute. All nodes in non-selected branches are marked as "not
selected" and skipped.

### Branch (ConditionalBranch)

One possible path from a router. Each branch has a condition (like `x == "red"`) and a **target node** — the first node
in that branch. Branches are evaluated in priority order; the first matching branch wins.

### Variable

A named value that can change during execution. Variables are defined at design time (VariableDefinition) and populated
at runtime (VariableContext). They're used by routers to make branching decisions and by skill properties for data flow.

---

## Execution Terms

### Execution Pipeline

The full sequence of steps from "start execution" to "all tasks complete."
See [Execution Pipeline](execution-pipeline.md) for the complete walkthrough.

### ExecutionOrchestrator

The top-level coordinator that manages an execution run. It initializes the schedule, validates preconditions, starts
the trigger service, then detaches the run on a background task. Registered as a **singleton** (for GraphQL subscription
continuity); each run's reactive state lives on a fresh `ExecutionSession`, and completion is single-phase.

### ExecutionTriggerService

Monitors execution events and triggers nodes when their prerequisites are met, delegating the actual firing to
`ISkillTriggerHandler` and `IRouterTriggerHandler`. A prerequisite is satisfied by the required event type or a `Failed`
or `NotSelected` event, so a failed or skipped upstream releases its dependents rather than stalling them.

### Scheduling / Rescheduling

**Scheduling** calculates when each task should start and how long it takes, using Google OR-Tools linear programming. *
*Rescheduling** happens during execution: when a skill finishes early or late, the schedule is recalculated so remaining
tasks adjust accordingly.

### Adaptive Execution

A skill execution mode where the duration isn't fixed. The skill is bounded below by a minimum (`MinAdaptiveDuration`)
and is unbounded above; it runs until an external "finish" signal arrives (another skill's terminal event). The schedule
is continuously rescheduled as the adaptive skill's duration grows. There is no maximum-duration cap in the control
plane — overruns surface only through the observability-only `AdaptiveSkillDurationOverrunMonitor`.

### DependencyGraph

A graph structure built from edges that maps each node to its **start prerequisites** and **finish prerequisites**. The
trigger service uses this graph to decide when a node is ready to execute.

### Source resolution

Expanding a dependency edge's endpoint to the executable nodes it stands for. A dependency can be drawn on a container
`TaskNode`, but only skills and routers actually fire, so the container is resolved to its executable descendants —
recursing through nested tasks to any depth and stopping at routers (a router resolves to itself, since it publishes its
own Start/Finish events). Implemented once by `INodeResolver` (`NodeResolver.ResolveToExecutableIds`) and shared by
`DependencyGraphAnalyzer` and `AgentSerializationValidator`.

---

## Technical / Pattern Terms

### BehaviorSubject

An Rx.NET concept: a subject that remembers its last emitted value and immediately sends it to new subscribers. Used
throughout Freydis for real-time state: when the frontend subscribes to "current nodes," it immediately gets the latest
list, then receives updates as they happen.

### Change Tracker

A single service, `ProcedureStateTracker`, implements all three change-tracker interfaces
(`INodeChangeTracker`, `IDependencyEdgeChangeTracker`, `IProcedureVariableChangeTracker`) over one
`BehaviorSubject<ProcedureState>`. When entities change, it pushes the new procedure-scoped state to all subscribers
(including GraphQL subscriptions); cross-procedure writes are filtered and logged.

### Singleton with Per-Execution State

The core architectural pattern. Services like `ExecutionOrchestrator` are registered as **singletons** (one instance for
the app's lifetime) so GraphQL subscriptions can hold references to them. But each run's mutable state (the reschedule
subject, subscriptions, completion task) lives on a fresh `ExecutionSession` (`IAsyncDisposable`); the session is the
single teardown sink and tears down via `OnCompleted` once the detached run finishes. This allows consecutive
"Execute → Stop → Execute" cycles without restarting the app.

See: [Architecture Overview](architecture.md)

### Event Bus (ISkillExecutionEventBus)

A reactive event stream where execution events are published. It exposes `AllEvents` plus type-filtered streams
`StartEvents`, `FinishEvents`, and `FailedEvents`. The trigger service, dispatcher, progress monitor, and rescheduling
coordinator subscribe to this bus.

### ExecutionEvent

A single event on the event bus. Contains:

- `SkillId` — The node this event is about
- `EventType` — Start, Finish, Progress, Failed, or NotSelected
- `Timestamp` — When it happened
- Progress/error detail fields (e.g. progress percentage, progress data, error message) for `Progress` and `Failed` events

### Fire-and-Forget (with safety net)

A pattern used when async work is kicked off without awaiting it (e.g., router evaluation from an Rx callback). The task
handles its own exceptions internally, and a `.ContinueWith(OnlyOnFaulted)` is attached as defense-in-depth to log any
truly unexpected escaping exception.

---

## Infrastructure Terms

### Repository

A class that handles reading and writing entities to/from the database. Freydis uses the **Repository Pattern** with a
generic base (`GenericPostgresRepository<T>`) and specialized adapters for polymorphic types (like nodes, which can be
TaskNode, SkillExecutionNode, or RouterNode).

### TypeHierarchyJsonConverter

A custom JSON serializer that handles polymorphic types. When a `Node` is stored as JSON, it adds a `$type`
discriminator field so the correct subtype (TaskNode, SkillExecutionNode, RouterNode) is reconstructed on read.

### CachedRepository

An `IMemoryCache`-backed decorator over `IRepository<T>` that sits between services and the database. Entities are cached
in memory for fast access; `CacheKeyGenerator` builds the keys and `MemoryCacheInvalidationService` clears them when
mutations modify the data.

---

## Related Documentation

- [Architecture Overview](architecture.md) — How all these concepts fit together
- [Execution Pipeline](execution-pipeline.md) — The execution flow in detail
- [Domain Layer](../Domain/docs/README.md) — Entity definitions and relationships
- [Documentation Hub](README.md) — Back to the index
