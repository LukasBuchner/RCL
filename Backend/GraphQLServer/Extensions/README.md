# GraphQL Server Extension Methods

This directory contains extension methods for configuring various services in the GraphQL server, following .NET best
practices.

## Extension Classes

### PostgresServiceExtensions

- Configures PostgreSQL persistence layer with Dapper
- Registers `PostgresDbContext` and all repositories as Singleton
- Supports reactive subscriptions via singleton lifetime

### ApplicationServiceExtensions

**Primary service registration class for the entire application layer.**

Registers all core application services with carefully chosen service lifetimes based on technical requirements and
architectural constraints. See the
detailed [ApplicationServiceExtensions Design Guide](#applicationserviceextensions-design-guide) section below for
comprehensive documentation of the DI strategy.

Key highlights:

- **Singleton services**: Orchestration, execution, scheduling, entity management, change trackers
- **Per-execution state management**: Singleton services create fresh state for each execution
- **GraphQL subscription support**: Requires persistent BehaviorSubjects across HTTP requests
- **Physical constraints**: Single IRuntimeAgent requires singleton coordination

### OrchestrationServiceExtensions

- Configures orchestration services including:
    - Graph building services
    - Scheduling services
    - Execution services
- Uses appropriate lifetimes (Singleton for stateless, Scoped for stateful)

### GraphQLServiceExtensions

- Configures HotChocolate GraphQL server
- Reads cost limits from configuration
- Registers GraphQL types and operations

### CorsServiceExtensions

- Configures CORS policy from configuration
- Supports multiple allowed origins

### OptionsValidationExtensions

- Validates configuration on startup
- Ensures required settings are present
- Uses data annotations for validation

### HealthCheckExtensions

- Adds comprehensive health checks for:
    - **PostgreSQL**: Connection status, database availability
    - **System**: Memory usage, uptime, environment details, GC statistics
    - **GraphQL API**: Endpoint availability
- Provides detailed health check endpoints:
    - `/health` - Full interactive HTML UI with detailed metrics (or JSON if requested)
    - `/status` - Simple JSON status for monitoring systems

### HealthCheckResponseWriter

- Custom response writer providing both HTML and JSON responses
- Beautiful, responsive HTML UI with:
    - Real-time status indicators with color coding
    - Auto-refresh every 30 seconds
    - Detailed system metrics and database statistics
    - Manual refresh capability
    - Mobile-friendly design

## Service Lifetimes

- **Singleton**: Used for orchestration, execution, scheduling, entity management, and change trackers
- **Scoped**: Used for repositories and database contexts (per-request state)
- **Transient**: Not currently used but available for lightweight stateless services

**IMPORTANT**: The service lifetime strategy in this application differs from typical ASP.NET Core applications. Most
application services are registered as Singleton, not Scoped. This is a deliberate architectural decision driven by
specific technical requirements. See
the [ApplicationServiceExtensions Design Guide](#applicationserviceextensions-design-guide) below for detailed
rationale.

## Configuration

All configuration is read from `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=FreydisDB;Username=postgres;Password=postgres"
  },
  "GraphQL": {
    "Cost": {
      "MaxFieldCost": 4000,
      "MaxTypeCost": 1000,
      "EnforceCostLimits": true,
      "ApplyCostDefaults": true,
      "DefaultResolverCost": 10.0
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174"
    ]
  }
}
```

---

## ApplicationServiceExtensions Design Guide

### Overview

The `ApplicationServiceExtensions` class is the central service registration point for the entire application layer. It
registers over 50 services with carefully chosen lifetimes based on specific technical constraints and architectural
requirements. **Most services are registered as Singleton**, which differs from typical ASP.NET Core patterns.

This design guide explains:

1. Why services are Singleton instead of Scoped
2. How per-execution state is managed in Singleton services
3. The technical constraints that drove these decisions
4. Code examples demonstrating the patterns

### Table of Contents

- [Critical Design Decisions](#critical-design-decisions)
- [Service Categories](#service-categories)
- [Why Singleton Instead of Scoped](#why-singleton-instead-of-scoped)
- [Per-Execution State Management Pattern](#per-execution-state-management-pattern)
- [GraphQL Subscription Requirements](#graphql-subscription-requirements)
- [Physical Device Constraints](#physical-device-constraints)
- [Code Examples](#code-examples)
- [Best Practices](#best-practices)

---

### Critical Design Decisions

#### 1. GraphQL Subscription Requirement

**Problem**: GraphQL subscriptions require persistent observable streams that survive across HTTP requests.

**Solution**: Services that expose `IObservable<T>` must be Singleton to maintain BehaviorSubjects across requests.

**Affected Services**:

- `INodeChangeTracker` / `ProcedureStateTracker`
- `IDependencyEdgeChangeTracker` / `ProcedureStateTracker`
- `IExecutionEventPublisher` / `ExecutionEventPublisher`
- `IExecutionOrchestrator` / `ExecutionOrchestrator`
- `ICrudSchedulingOrchestrator` / `CrudSchedulingOrchestrator`

**Why Not Scoped**: Scoped services are recreated per HTTP request. GraphQL subscriptions use Server-Sent Events (SSE)
or WebSockets that span multiple HTTP requests. If change trackers were Scoped, each subscription request would get a
different BehaviorSubject instance, breaking real-time notifications.

#### 2. Physical Device Constraint

**Problem**: The system controls a single physical robot agent (IRuntimeAgent). Multiple concurrent executions on the
same physical device would cause conflicts.

**Solution**: Execution orchestration services are Singleton to enforce sequential execution on the shared physical
resource.

**Affected Services**:

- `IExecutionOrchestrator` / `ExecutionOrchestrator`
- `ISkillExecutionCoordinator` / `SkillExecutionCoordinator`
- `IRuntimeAgentProvider` / `RuntimeAgentProvider`
- `IAgentManager` / `UnifiedAgentManager`

**Why Not Scoped**: Scoped services could allow concurrent executions from different HTTP requests to both try to
control the physical agent simultaneously, causing undefined behavior.

#### 3. Consecutive Execution Support

**Problem**: The Singleton ExecutionOrchestrator must support multiple consecutive procedure executions without
requiring service recreation.

**Solution**: Create per-execution state (Subjects, dictionaries, subscriptions) at the start of each execution and
dispose it in finally blocks.

**Affected Services**: All execution services that manage execution-specific state.

**Why This Works**: Fresh Rx.NET Subjects and collections are created for each execution, providing isolated state even
though the service instance is reused.

#### 4. Entity Management Consistency

**Problem**: Entity management services (Nodes, Edges, Agents, Skills, etc.) coordinate with change trackers that must
be Singleton.

**Solution**: Register entity management services as Singleton to match the lifetime of their dependent change trackers.

**Affected Services**:

- `INodeApplicationService` / `NodeApplicationService`
- `IDependencyEdgeApplicationService` / `DependencyEdgeApplicationService`
- `IAgentApplicationService` / `AgentApplicationService`
- `ISkillApplicationService` / `SkillApplicationService`
- `IPositionTagApplicationService` / `PositionTagApplicationService`
- `ISceneObjectApplicationService` / `SceneObjectApplicationService`

**Why Singleton**: Ensures entity services always interact with the same change tracker instances that feed GraphQL
subscriptions.

---

### Service Categories

Services in `ApplicationServiceExtensions` are organized into logical categories:

#### 1. Scheduling Services

**Lifetime**: Singleton

Calculate timing, positions, and dependencies for procedure nodes.

Services:

- `ITimingCalculationOrchestrator` - Orchestrates scheduling pipeline
- `INodeHierarchyProcessor` - Processes node hierarchies
- `ITimingCalculationEngine` - Performs timing calculations
- `IDurationProviderFactory` - Creates duration providers
- `INodePositioningService` - Calculates node UI positions
- `ITimingAnalyzer` - Analyzes timing statistics
- `ISchedulingPhaseLogger` - Logs scheduling phases
- `ISchedulingResultLogger` - Logs scheduling results

Supporting services:

- `ITaskNodeDurationCalculator` - Calculates task durations
- `ITimingAggregator` - Aggregates timing data
- `IHierarchicalSorter` - Sorts hierarchical structures
- `IChildNodeCollector` - Collects child nodes
- `INodeTimingMapper` - Maps node timing data
- `INodeDurationAdjuster` - Adjusts node durations
- `IHierarchyValidator` - Validates hierarchies
- `INodeRelationshipMapper` - Maps node relationships
- `IScheduleResultConverter` - Converts schedule results
- `ITimingStatisticsCollector` - Collects timing statistics

#### 2. Execution Services

**Lifetime**: Singleton

**Critical**: These services MUST be Singleton for physical device coordination and GraphQL subscriptions.

Services:

- `IExecutionOrchestrator` - Main execution coordinator
- `ISkillExecutionEventBus` - Central event bus (Rx.NET Subject)
- `IDependencyGraphAnalyzer` - Analyzes execution dependencies
- `ISkillExecutionCoordinator` - Coordinates skill execution on agents
- `IExecutionTriggerService` - Triggers skills when prerequisites met
- `IExecutionIdAssigner` - Assigns execution IDs
- `ISkillExecutionStateManager` - Manages execution state
- `IExecutionStateTransitionService` - Handles state transitions
- `IExecutionEventPublisher` - Publishes execution events
- `IExecutionProgressMonitor` - Monitors execution progress
- `IExecutionInitializer` - Initializes executions

Rescheduling services:

- `IReschedulingCoordinator` - Coordinates rescheduling
- `IExecutionProgressDataBuilder` - Builds progress data
- `IExecutionTimeCalculator` - Calculates execution times

**Per-Execution State Pattern**: All execution services create fresh state for each execution in
`StartLoadedProcedureAsync` and dispose it in finally blocks.

#### 3. Entity Management Services

**Lifetime**: Singleton

Provide CRUD operations for domain entities with integrated change tracking.

Services:

- `INodeApplicationService` - Node CRUD with change tracking
- `IDependencyEdgeApplicationService` - Edge CRUD with change tracking
- `IAgentApplicationService` - Agent CRUD with change tracking
- `ISkillApplicationService` - Skill CRUD with change tracking
- `IPositionTagApplicationService` - Position tag CRUD with change tracking
- `ISceneObjectApplicationService` - Scene object CRUD with change tracking

**Why Singleton**: Must interact with Singleton change trackers for consistent GraphQL subscriptions.

#### 4. Change Trackers (Reactive Infrastructure)

**Lifetime**: Singleton (REQUIRED)

**Critical**: Change trackers MUST be Singleton for GraphQL subscriptions to work.

Services:

- `INodeChangeTracker` / `ProcedureStateTracker`
- `IDependencyEdgeChangeTracker` / `ProcedureStateTracker`

**Technical Details**:

- Maintain `BehaviorSubject<IReadOnlyList<TEntity>>` for reactive streams
- Expose `IObservable<IReadOnlyList<TEntity>>` for GraphQL subscriptions
- Load initial data from repositories on startup
- Receive updates via event handlers from orchestrators
- Provide immediate values to new subscribers (BehaviorSubject behavior)

**Why Not Scoped**: Scoped change trackers would create new BehaviorSubject instances per HTTP request, breaking
real-time notifications across subscription connections.

#### 5. Agent Coordination Services

**Lifetime**: Singleton

Manage agent discovery, registration, and skill synchronization.

Services:

- `ISceneEntityProvider` - Provides scene entities
- `IAgentDiscoveryService` - Discovers available agents
- `IAgentRegistrationService` - Registers agents
- `ISkillSynchronizationService` - Synchronizes skills
- `IAgentSkillRelationshipManager` - Manages agent-skill relationships
- `ISkillCapabilityDiscoveryService` - Discovers skill capabilities
- `INodeAgentMapper` - Maps nodes to agents
- `IAgentCapabilityAnalyzer` - Analyzes agent capabilities

Agent manager services:

- `IDummyAgentFactory` - Factory for creating dummy agents
- `IKukaAgentFactory` - Factory for creating KUKA agents
- `IAgentManager` - Main agent manager interface (UnifiedAgentManager)
- `IRuntimeAgentProvider` - Provides runtime agent (physical constraint)

#### 6. UI and Position Calculation Services

**Lifetime**: Singleton

Calculate node positions for UI layout.

Services:

- `INodePositionXCalculator` - Calculates X positions
- `INodePositionYCalculator` - Calculates Y positions
- `INodeHeightCalculator` - Calculates node heights

#### 7. Infrastructure Services

**Lifetime**: Singleton

Core infrastructure and shared services.

Services:

- `IRclProcedureQueryService` - Procedure queries
- `ICrudSchedulingOrchestrator` - CRUD with scheduling
- `IGraphQLMapperService` - GraphQL DTO mapping
- `PlanningModeDurationProvider` - Planning mode durations

---

### Why Singleton Instead of Scoped

#### Typical ASP.NET Core Pattern (Not Used Here)

In most ASP.NET Core applications, services are registered as **Scoped**:

```csharp
services.AddScoped<IMyService, MyService>();
```

**Scoped Lifetime**:

- New instance created per HTTP request
- Instance shared within that request
- Automatically disposed at end of request
- Prevents state leakage between requests

**Why Scoped is Common**: Works well for stateless services or request-specific state (e.g., DbContext, authentication
context).

#### Why This Application Uses Singleton

This application registers most services as **Singleton** for specific technical reasons:

```csharp
services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
services.AddSingleton<INodeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
```

**Singleton Lifetime**:

- Single instance for application lifetime
- Shared across all requests
- Never automatically disposed (lives until app shutdown)
- Requires manual state management

**Reasons for Singleton**:

1. **GraphQL Subscriptions**: Require persistent BehaviorSubjects
2. **Physical Device**: Single robot requires coordination
3. **Change Tracking**: Consistent reactive streams across requests
4. **Performance**: Avoid recreating heavy services per request

#### Comparison Table

| Aspect                 | Scoped (Typical)                 | Singleton (This App)         |
|------------------------|----------------------------------|------------------------------|
| Instance Creation      | Per HTTP request                 | Application startup          |
| State Isolation        | Automatic                        | Manual per-execution         |
| GraphQL Subscriptions  | Broken (new Subject per request) | Working (persistent Subject) |
| Physical Device Access | Risk of concurrent access        | Sequential enforcement       |
| Performance            | Overhead of recreation           | Single instance reused       |
| Memory                 | Released per request             | Lives until shutdown         |

---

### Per-Execution State Management Pattern

Singleton services that manage execution-specific state follow a strict pattern to support consecutive executions
without requiring service recreation.

#### The Pattern

```csharp
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // Singleton dependencies (injected once)
    private readonly ILogger<ExecutionOrchestrator> _logger;
    private readonly IExecutionInitializer _executionInitializer;

    // Per-execution state (created fresh each execution)
    private Subject<string>? _rescheduleRequests;
    private IDisposable? _rescheduleSubscription;
    private IDisposable? _eventBusSubscription;
    private volatile bool _isCleaningUp;

    public async Task<bool> StartLoadedProcedureAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 1. CREATE per-execution state (fresh for this execution)
            _rescheduleRequests = new Subject<string>();
            _isCleaningUp = false;

            // 2. SET UP reactive pipeline
            _rescheduleSubscription = _rescheduleRequests
                .Sample(TimeSpan.FromMilliseconds(500))
                .Select(reason => Observable.FromAsync(async ct =>
                {
                    // Delegate to coordinator
                    var result = await _reschedulingCoordinator.RescheduleAsync(reason, ct);
                    // ... handle result
                    return result;
                }))
                .Merge(maxConcurrent: 1)
                .Subscribe(
                    _ => CheckExecutionCompletion(),
                    error => _logger.LogError(error, "Error in re-schedule stream"));

            // 3. INITIALIZE execution context
            var initResult = await _executionInitializer.InitializeAsync(
                _executionStartTime, cancellationToken);

            // 4. EXECUTE the procedure
            _executionTriggerService.Start(dependencyGraph, _currentNodes);

            return await _executionCompletion.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start execution");
            return false;
        }
        finally
        {
            // 5. CLEANUP per-execution state (critical!)
            _isCleaningUp = true;  // Prevent OnNext on disposed Subject

            _executionTriggerService.Stop();
            _eventBusSubscription?.Dispose();
            _rescheduleSubscription?.Dispose();

            _rescheduleRequests?.Dispose();
            _rescheduleRequests = null;  // Clear reference
        }
    }
}
```

#### Key Elements

1. **Per-Execution Fields**: Nullable fields that hold execution-specific state
   ```csharp
   private Subject<string>? _rescheduleRequests;
   private IDisposable? _rescheduleSubscription;
   ```

2. **Creation on Entry**: Fresh state created at method start
   ```csharp
   _rescheduleRequests = new Subject<string>();
   ```

3. **Cleanup Flag**: Prevents operations on disposed objects
   ```csharp
   private volatile bool _isCleaningUp;

   // Before using Subject, check flag
   if (!_isCleaningUp && _rescheduleRequests != null)
   {
       _rescheduleRequests.OnNext("event");
   }
   ```

4. **Finally Block**: Guaranteed cleanup even on exceptions
   ```csharp
   finally
   {
       _isCleaningUp = true;
       _rescheduleSubscription?.Dispose();
       _rescheduleRequests?.Dispose();
       _rescheduleRequests = null;
   }
   ```

#### State Manager Example

The `SkillExecutionStateManager` demonstrates per-execution state in a Singleton:

```csharp
public class SkillExecutionStateManager : ISkillExecutionStateManager
{
    // Per-execution state (cleared and repopulated each execution)
    private readonly ConcurrentDictionary<Guid, SkillExecutionState> _skillStates = new();
    private readonly ConcurrentDictionary<Guid, IRuntimeAgent> _agentAssignments = new();

    public void Initialize(
        IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, IRuntimeAgent> agentAssignments)
    {
        // CLEAR previous execution state
        _skillStates.Clear();
        _agentAssignments.Clear();

        // POPULATE with new execution data
        var skillExecutionNodes = nodes.OfType<SkillExecutionNode>().ToList();

        foreach (var skillNode in skillExecutionNodes)
        {
            _skillStates[skillNode.Id] = new SkillExecutionState(skillNode);

            if (agentAssignments.TryGetValue(skillNode.Id, out var agent))
            {
                _agentAssignments[skillNode.Id] = agent;
            }
        }
    }

    public SkillExecutionState? GetState(Guid skillId)
    {
        return _skillStates.GetValueOrDefault(skillId);
    }
}
```

**Key Points**:

- ConcurrentDictionary provides thread-safe state storage
- `Initialize` clears previous execution state
- Fresh state populated for each execution
- Service instance reused, state is not

---

### GraphQL Subscription Requirements

GraphQL subscriptions are a critical feature requiring Singleton services.

#### How GraphQL Subscriptions Work

1. Client sends subscription request via Server-Sent Events (SSE) or WebSocket
2. Server creates a long-lived connection
3. Server pushes updates to client whenever data changes
4. Connection spans multiple HTTP requests

#### The BehaviorSubject Requirement

GraphQL subscriptions in HotChocolate are backed by `IObservable<T>`:

```csharp
// GraphQL Subscription Operation
public class Subscription
{
    [Subscribe]
    public IObservable<IReadOnlyList<Node>> NodesChanged(
        [Service] ICrudSchedulingOrchestrator orchestrator)
    {
        return orchestrator.NodesChanged;  // Must be persistent!
    }
}
```

The observable comes from a BehaviorSubject in the change tracker:

```csharp
public class ProcedureStateTracker
    : INodeChangeTracker, IDependencyEdgeChangeTracker, IProcedureVariableChangeTracker, IProcedureStateScope
{
    private readonly BehaviorSubject<ProcedureState> _stateSubject = new(ProcedureState.Empty);

    public IObservable<IReadOnlyList<Node>> Nodes =>
        _stateSubject.Select(s => s.Nodes).DistinctUntilChanged();

    public void UpdateNodes(IReadOnlyList<Node> scoped)
    {
        _stateSubject.OnNext(_stateSubject.Value with { Nodes = scoped });  // Push to subscribers
    }
}
```

#### Why Scoped Breaks Subscriptions

If `ProcedureStateTracker` were registered as Scoped:

1. **Initial Subscription Request** (Request A):
    - New `ProcedureStateTracker` instance created
    - New `BehaviorSubject` instance A created
    - Subscription connected to Subject A
    - Request A ends, Scoped service disposed

2. **CRUD Operation** (Request B):
    - New `ProcedureStateTracker` instance created
    - New `BehaviorSubject` instance B created
    - State update called, pushes to Subject B
    - **Subscription still listening to Subject A (no updates!)**

**Result**: GraphQL subscription never receives updates because it's listening to a different BehaviorSubject instance
than the one receiving updates.

#### Why Singleton Works

With Singleton `ProcedureStateTracker`:

1. **Initial Subscription Request** (Request A):
    - Singleton `ProcedureStateTracker` returned
    - Subscribe to its persistent `BehaviorSubject`
    - Request A ends, service continues to live

2. **CRUD Operation** (Request B):
    - Same Singleton `ProcedureStateTracker` returned
    - Same `BehaviorSubject` updated
    - **All subscribers receive updates**

**Result**: GraphQL subscriptions work because all requests share the same BehaviorSubject instance.

#### Event Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Singleton Services                       │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         CrudSchedulingOrchestrator                   │  │
│  │  (Handles CRUD operations)                           │  │
│  └────────┬────────────────────────────────────┬────────┘  │
│           │ Raises event                       │           │
│           │ NodeDataChanged                    │           │
│           ▼                                    │           │
│  ┌──────────────────────────────────────────┐ │           │
│  │       ProcedureStateTracker             │ │           │
│  │  ┌──────────────────────────────────┐   │ │           │
│  │  │  BehaviorSubject<Nodes>          │   │ │           │
│  │  │  (Persistent across requests)    │   │ │           │
│  │  └──────────┬───────────────────────┘   │ │           │
│  │             │ OnNext(nodes)              │ │           │
│  └─────────────┼────────────────────────────┘ │           │
│                │                               │           │
│                │ Publishes to all subscribers  │           │
│                ▼                               │           │
│  ┌───────────────────────────────┐            │           │
│  │  GraphQL Subscription Client   │◄───────────┘           │
│  │  (SSE/WebSocket connection)    │                        │
│  └───────────────────────────────┘                        │
└─────────────────────────────────────────────────────────────┘

Timeline:
1. Client subscribes → Connected to BehaviorSubject
2. CRUD operation → Raises NodeDataChanged event
3. ProcedureStateTracker receives event → Calls OnNext on BehaviorSubject
4. BehaviorSubject pushes to ALL subscribers → Client receives update
```

---

### Physical Device Constraints

The application controls a single physical robot agent, creating a critical constraint.

#### The Problem

Multiple concurrent procedure executions would attempt to control the same physical device:

```
Request A: Start execution → Move robot to position X
Request B: Start execution → Move robot to position Y (conflicts!)
```

**Result**: Undefined behavior, potential physical damage to robot.

#### The Solution: Singleton Execution Orchestrator

```csharp
services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
services.AddSingleton<IRuntimeAgentProvider>(provider =>
    new RuntimeAgentProvider(
        provider.GetRequiredService<IAgentManager>()));
```

The Singleton orchestrator enforces sequential execution:

```csharp
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    private readonly IRuntimeAgentProvider _runtimeAgentProvider;
    private TaskCompletionSource<bool> _executionCompletion = new();

    public async Task<bool> StartLoadedProcedureAsync(CancellationToken ct)
    {
        // Only ONE execution can run at a time
        // New execution request waits until _executionCompletion is signaled

        try
        {
            // Initialize execution with THE SINGLE RUNTIME AGENT
            var agent = _runtimeAgentProvider.GetRuntimeAgent();

            // Execute skills on the agent
            await _skillExecutionCoordinator.ExecuteSkillAsync(skillNode, agent);

            return await _executionCompletion.Task;
        }
        finally
        {
            // Signal completion, allowing next execution to start
            _executionCompletion.TrySetResult(true);
        }
    }
}
```

#### Why Not Scoped

If `ExecutionOrchestrator` were Scoped:

1. **Request A**: Execute Procedure 1
    - Scoped orchestrator instance A created
    - Starts execution on physical robot

2. **Request B**: Execute Procedure 2 (concurrent)
    - Scoped orchestrator instance B created
    - **Also starts execution on the SAME physical robot**

**Result**: Both instances try to control the robot simultaneously, causing conflicts.

With Singleton:

1. **Request A**: Execute Procedure 1
    - Singleton orchestrator starts execution

2. **Request B**: Execute Procedure 2 (concurrent)
    - Same Singleton orchestrator instance
    - Must wait for execution A to complete

**Result**: Sequential execution on physical device, no conflicts.

---

### Code Examples

#### Example 1: Change Tracker with BehaviorSubject

```csharp
/// <summary>
/// Singleton tracker for nodes, edges, and variables of the loaded procedure.
/// Holds one persistent BehaviorSubject so GraphQL subscriptions survive across requests.
/// </summary>
public class ProcedureStateTracker
    : INodeChangeTracker, IDependencyEdgeChangeTracker, IProcedureVariableChangeTracker, IProcedureStateScope
{
    // CRITICAL: Persistent BehaviorSubject for GraphQL subscriptions
    private readonly BehaviorSubject<ProcedureState> _stateSubject = new(ProcedureState.Empty);

    /// <summary>
    /// Observable stream of node changes for the loaded procedure.
    /// New subscribers immediately receive the latest value (BehaviorSubject).
    /// </summary>
    public IObservable<IReadOnlyList<Node>> Nodes =>
        _stateSubject.Select(s => s.Nodes).DistinctUntilChanged();

    /// <summary>
    /// Observable stream of edge changes for the loaded procedure.
    /// </summary>
    public IObservable<IReadOnlyList<DependencyEdge>> Edges =>
        _stateSubject.Select(s => s.Edges).DistinctUntilChanged();

    /// <summary>
    /// Replaces the procedure-scoped nodes and pushes the new state to all subscribers.
    /// </summary>
    public void UpdateNodes(IReadOnlyList<Node> scoped)
    {
        // Push the next immutable snapshot to all subscribers
        _stateSubject.OnNext(_stateSubject.Value with { Nodes = scoped });
    }

    /// <summary>
    /// Loads the procedure-scoped nodes, edges, and variables when a procedure is opened.
    /// </summary>
    public async Task OnProcedureLoaded(Guid procedureId)
    {
        var nodes = await _repository.GetNodesAsync(procedureId);
        var edges = await _repository.GetEdgesAsync(procedureId);
        _stateSubject.OnNext(_stateSubject.Value with { Nodes = nodes, Edges = edges });
    }

    /// <summary>
    /// Clears all streams back to the empty state when the procedure is closed.
    /// </summary>
    public void OnProcedureUnloaded() => _stateSubject.OnNext(ProcedureState.Empty);
}
```

**Key Points**:

- `BehaviorSubject` initialized once at construction with `ProcedureState.Empty`
- `Nodes`, `Edges`, and `Variables` project the same single state subject for all requests
- State updates push a new immutable `ProcedureState` snapshot to all subscribers via OnNext
- Singleton lifetime ensures all requests share the same BehaviorSubject

#### Example 2: CRUD Orchestrator with Event-Driven Change Tracking

```csharp
/// <summary>
/// CRUD operations orchestrator with integrated scheduling and reactive notifications.
/// SINGLETON: Required to coordinate with Singleton change trackers.
/// </summary>
public class CrudSchedulingOrchestrator : ICrudSchedulingOrchestrator
{
    private readonly IRepository<Node> _nodeRepository;
    private readonly INodeChangeTracker _nodeChangeTracker;  // Singleton
    private readonly ITimingCalculationOrchestrator _timingCalculationOrchestrator;
    private readonly ILogger<CrudSchedulingOrchestrator> _logger;

    /// <summary>
    /// Event raised when nodes change in the system.
    /// Change tracker subscribes to this event.
    /// </summary>
    public event EventHandler<EntityChangedEventArgs<Node>>? NodeDataChanged;

    public CrudSchedulingOrchestrator(
        IRepository<Node> nodeRepository,
        INodeChangeTracker nodeChangeTracker,
        ITimingCalculationOrchestrator timingCalculationOrchestrator,
        ILogger<CrudSchedulingOrchestrator> logger)
    {
        _nodeRepository = nodeRepository;
        _nodeChangeTracker = nodeChangeTracker;
        _timingCalculationOrchestrator = timingCalculationOrchestrator;
        _logger = logger;

        // Wire up event to change tracker (event-to-observable adapter pattern)
        NodeDataChanged += (_, args) => _nodeChangeTracker.UpdateEntities(args.Entities);
    }

    /// <summary>
    /// Observable for real-time node changes.
    /// Delegates to change tracker's persistent BehaviorSubject.
    /// </summary>
    public IObservable<IReadOnlyList<Node>> NodesChanged => _nodeChangeTracker.Nodes;

    /// <summary>
    /// Creates a node with automatic scheduling and change notification.
    /// </summary>
    public async Task<Node> CreateNodeAsync(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // 1. Create node in repository
        var createdNode = await _nodeRepository.CreateAsync(node);

        // 2. Trigger scheduling recalculation
        var scheduleResult = await _timingCalculationOrchestrator.CalculateTimingAsync(...);

        // 3. Update repository with calculated timing
        await UpdateNodesWithTimingAsync(scheduleResult.UpdatedNodes);

        // 4. Notify subscribers via event
        var allNodes = await _nodeRepository.GetAllAsync();
        NodeDataChanged?.Invoke(this, new EntityChangedEventArgs<Node>(allNodes));

        return createdNode;
    }
}
```

**Event Flow**:

1. CRUD operation completes
2. Orchestrator raises `NodeDataChanged` event
3. Event handler calls `_nodeChangeTracker.UpdateEntities()`
4. Change tracker calls `_entitiesSubject.OnNext()`
5. BehaviorSubject pushes to all GraphQL subscribers

#### Example 3: Execution Orchestrator with Per-Execution State

```csharp
/// <summary>
/// Orchestrates the execution of procedures.
/// SINGLETON: Required for GraphQL subscriptions and physical device coordination.
/// Supports consecutive executions via per-execution state management.
/// </summary>
public class ExecutionOrchestrator : IExecutionOrchestrator
{
    // Singleton dependencies (injected once, never change)
    private readonly ILogger<ExecutionOrchestrator> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IExecutionInitializer _executionInitializer;
    private readonly ISkillExecutionStateManager _stateManager;
    private readonly IExecutionTriggerService _executionTriggerService;
    private readonly ISkillExecutionEventBus _eventBus;
    private readonly IReschedulingCoordinator _reschedulingCoordinator;

    // Per-execution state (created fresh for each execution)
    private TaskCompletionSource<bool> _executionCompletion = new();
    private IReadOnlyList<Node> _currentNodes = new List<Node>();
    private IReadOnlyList<DependencyEdge> _currentEdges = new List<DependencyEdge>();
    private ScheduleResult? _currentSchedule;
    private DateTimeOffset _executionStartTime;
    private IDisposable? _eventBusSubscription;
    private Guid _procedureId;

    // Rx.NET Subject for reschedule requests (created per-execution)
    private Subject<string>? _rescheduleRequests;
    private IDisposable? _rescheduleSubscription;
    private volatile bool _isCleaningUp;

    /// <summary>
    /// Event raised when nodes change during execution.
    /// Used by ExecutionEventPublisher to push to GraphQL subscriptions.
    /// </summary>
    public event EventHandler<EntityChangedEventArgs<Node>>? NodeDataChanged;

    public ExecutionOrchestrator(
        ILogger<ExecutionOrchestrator> logger,
        TimeProvider timeProvider,
        IExecutionInitializer executionInitializer,
        ISkillExecutionStateManager stateManager,
        IExecutionTriggerService executionTriggerService,
        ISkillExecutionEventBus eventBus,
        IReschedulingCoordinator reschedulingCoordinator)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _executionInitializer = executionInitializer;
        _stateManager = stateManager;
        _executionTriggerService = executionTriggerService;
        _eventBus = eventBus;
        _reschedulingCoordinator = reschedulingCoordinator;

        // Wire up events to event publisher (persistent across executions)
        NodeDataChanged += (_, args) => _eventPublisher.PublishNodeChanges(args.Entities);
    }

    /// <summary>
    /// Starts execution of a loaded procedure.
    /// SUPPORTS CONSECUTIVE EXECUTIONS via per-execution state management.
    /// </summary>
    public async Task<bool> StartLoadedProcedureAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting event-driven execution of loaded procedure");

        try
        {
            // ═══════════════════════════════════════════════════════════
            // STEP 1: CREATE per-execution state (fresh for this execution)
            // ═══════════════════════════════════════════════════════════

            _rescheduleRequests = new Subject<string>();  // NEW Subject
            _isCleaningUp = false;  // Reset cleanup flag

            // ═══════════════════════════════════════════════════════════
            // STEP 2: SET UP reactive pipeline for rescheduling
            // ═══════════════════════════════════════════════════════════

            _rescheduleSubscription = _rescheduleRequests
                .Sample(TimeSpan.FromMilliseconds(500))  // Throttle requests
                .Select(reason => Observable.FromAsync(async ct =>
                {
                    // Delegate to ReschedulingCoordinator (SRP)
                    var result = await _reschedulingCoordinator.RescheduleAsync(reason, ct);

                    // Update local nodes if successful
                    if (!result.Success || result.UpdatedNodes == null)
                        return result;

                    _currentNodes = result.UpdatedNodes;
                    PublishNodeChanges(_currentNodes);

                    return result;
                }))
                .Merge(maxConcurrent: 1)  // Serialize rescheduling operations
                .Subscribe(
                    _ => CheckExecutionCompletion(),
                    error => _logger.LogError(error, "Error in re-schedule stream"));

            // ═══════════════════════════════════════════════════════════
            // STEP 3: INITIALIZE execution context
            // ═══════════════════════════════════════════════════════════

            _executionStartTime = _timeProvider.GetUtcNow();

            var initResult = await _executionInitializer.InitializeAsync(
                _executionStartTime, cancellationToken);

            if (!initResult.Success)
            {
                _logger.LogError("Failed to initialize execution: {ErrorMessage}",
                    initResult.ErrorMessage);
                return false;
            }

            // Store initialized data
            _currentNodes = initResult.Nodes;
            _currentEdges = initResult.Edges;
            _currentSchedule = initResult.Schedule;
            _procedureId = Guid.Empty;

            // Publish initial state to frontend
            PublishNodeChanges(_currentSchedule?.UpdatedNodes ?? _currentNodes);

            _executionCompletion = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => _executionCompletion.TrySetCanceled());

            // Initialize state manager with nodes and agent assignments
            _stateManager.Initialize(_currentSchedule?.UpdatedNodes ?? _currentNodes,
                initResult.AgentAssignments);

            // Initialize rescheduling coordinator
            _reschedulingCoordinator.Initialize(_procedureId, _currentNodes,
                _currentEdges, _executionStartTime);

            // ═══════════════════════════════════════════════════════════
            // STEP 4: ANALYZE dependencies and start execution
            // ═══════════════════════════════════════════════════════════

            var dependencyGraph = _dependencyGraphAnalyzer.AnalyzeDependencies(
                _currentNodes, _currentEdges);

            // Subscribe to event bus to track execution events
            _eventBusSubscription = _eventBus.AllEvents.Subscribe(
                OnExecutionEvent,
                error => _logger.LogError(error, "Error in event bus stream"));

            // Start event-driven execution trigger service
            _executionTriggerService.Start(dependencyGraph, _currentNodes);

            // ═══════════════════════════════════════════════════════════
            // STEP 5: WAIT for execution to complete
            // ═══════════════════════════════════════════════════════════

            return await _executionCompletion.Task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start execution of loaded procedure");
            return false;
        }
        finally
        {
            // ═══════════════════════════════════════════════════════════
            // STEP 6: CLEANUP per-execution state (CRITICAL!)
            // ═══════════════════════════════════════════════════════════

            _logger.LogInformation("Cleaning up event-driven execution");

            // Set cleanup flag BEFORE disposing to prevent OnNext on disposed Subject
            _isCleaningUp = true;

            // Stop triggering service (prevents new skills from starting)
            _executionTriggerService.Stop();

            // Dispose event subscriptions (prevents new events)
            _eventBusSubscription?.Dispose();
            _rescheduleSubscription?.Dispose();

            // Dispose the Subject and clear reference (now safe)
            _rescheduleRequests?.Dispose();
            _rescheduleRequests = null;

            // Service instance remains alive (Singleton)
            // Can be used for next execution with fresh state
        }
    }

    /// <summary>
    /// Handles execution events from the event bus.
    /// Called for each skill start/finish/progress event.
    /// </summary>
    private void OnExecutionEvent(ExecutionEvent executionEvent)
    {
        _logger.LogDebug("Received {EventType} event for skill {SkillId}",
            executionEvent.EventType, executionEvent.SkillId);

        // Find the skill node
        var skillNode = _currentNodes.OfType<SkillExecutionNode>()
            .FirstOrDefault(n => n.Id == executionEvent.SkillId);

        if (skillNode == null)
        {
            _logger.LogWarning("Skill node not found for event {SkillId}",
                executionEvent.SkillId);
            return;
        }

        switch (executionEvent.EventType)
        {
            case ExecutionEventType.Start:
                _stateTransitionService.TransitionToRunning(
                    skillNode.Id, agent, executionEvent.Timestamp);

                // Request rescheduling (throttled via Rx.NET)
                // Check cleanup flag to prevent OnNext on disposed Subject
                if (!_isCleaningUp && _rescheduleRequests != null)
                {
                    _rescheduleRequests.OnNext("Skill started");
                }
                break;

            case ExecutionEventType.Finish:
                _stateTransitionService.TransitionToCompleted(
                    skillNode.Id, executionEvent.Timestamp);

                // Request rescheduling (throttled via Rx.NET)
                if (!_isCleaningUp && _rescheduleRequests != null)
                {
                    _rescheduleRequests.OnNext("Skill finished");
                }
                break;
        }
    }

    /// <summary>
    /// Publishes node changes to the frontend via event publisher.
    /// </summary>
    private void PublishNodeChanges(IReadOnlyList<Node> updatedNodes)
    {
        NodeDataChanged?.Invoke(this, new EntityChangedEventArgs<Node>(updatedNodes));
    }
}
```

**Key Patterns Demonstrated**:

1. **Per-Execution State Creation**:
   ```csharp
   _rescheduleRequests = new Subject<string>();  // Fresh Subject
   _isCleaningUp = false;  // Reset flag
   ```

2. **Cleanup Flag Pattern**:
   ```csharp
   if (!_isCleaningUp && _rescheduleRequests != null)
   {
       _rescheduleRequests.OnNext("event");
   }
   ```

3. **Guaranteed Cleanup**:
   ```csharp
   finally
   {
       _isCleaningUp = true;
       _executionTriggerService.Stop();
       _eventBusSubscription?.Dispose();
       _rescheduleSubscription?.Dispose();
       _rescheduleRequests?.Dispose();
       _rescheduleRequests = null;
   }
   ```

4. **Service Reusability**:
    - Service instance lives forever (Singleton)
    - State is recreated for each execution
    - Multiple consecutive executions supported

#### Example 4: Service Registration

```csharp
/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ══════════════════════════════════════════════════════════════
        // CHANGE TRACKERS - MUST BE SINGLETON
        // ══════════════════════════════════════════════════════════════
        // Required for GraphQL subscriptions with persistent BehaviorSubjects

        services.AddSingleton<ProcedureStateTracker>();
        services.AddSingleton<IProcedureStateScope>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<INodeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IDependencyEdgeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IProcedureVariableChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());

        // ══════════════════════════════════════════════════════════════
        // EXECUTION SERVICES - MUST BE SINGLETON
        // ══════════════════════════════════════════════════════════════
        // Required for:
        // 1. GraphQL subscriptions (persistent observables)
        // 2. Physical device coordination (single IRuntimeAgent)
        // 3. Per-execution state management (supports consecutive executions)

        services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
        services.AddSingleton<ISkillExecutionEventBus, SkillExecutionEventBus>();
        services.AddSingleton<IDependencyGraphAnalyzer, DependencyGraphAnalyzer>();
        services.AddSingleton<ISkillExecutionCoordinator, SkillExecutionCoordinator>();
        services.AddSingleton<IExecutionTriggerService, ExecutionTriggerService>();
        services.AddSingleton<IExecutionIdAssigner, ExecutionIdAssigner>();
        services.AddSingleton<ISkillExecutionStateManager, SkillExecutionStateManager>();
        services.AddSingleton<IExecutionStateTransitionService, ExecutionStateTransitionService>();
        services.AddSingleton<IExecutionEventPublisher, ExecutionEventPublisher>();
        services.AddSingleton<IExecutionProgressMonitor, ExecutionProgressMonitor>();
        services.AddSingleton<IExecutionInitializer, ExecutionInitializer>();

        // ══════════════════════════════════════════════════════════════
        // SCHEDULING SERVICES - SINGLETON FOR CONSISTENCY
        // ══════════════════════════════════════════════════════════════
        // Singleton to coordinate with Singleton change trackers

        services.AddSingleton<ITimingCalculationOrchestrator, TimingCalculationOrchestrator>();
        services.AddSingleton<ICrudSchedulingOrchestrator, CrudSchedulingOrchestrator>();
        services.AddSingleton<IDurationProviderFactory, DurationProviderFactory>();
        services.AddSingleton<INodePositioningService, NodePositioningService>();
        services.AddSingleton<ITimingAnalyzer, TimingAnalyzer>();

        // ══════════════════════════════════════════════════════════════
        // ENTITY MANAGEMENT SERVICES - SINGLETON
        // ══════════════════════════════════════════════════════════════
        // Singleton to match lifetime of change trackers

        services.AddSingleton<INodeApplicationService, NodeApplicationService>();
        services.AddSingleton<IDependencyEdgeApplicationService, DependencyEdgeApplicationService>();
        services.AddSingleton<IAgentApplicationService, AgentApplicationService>();
        services.AddSingleton<ISkillApplicationService, SkillApplicationService>();

        // ══════════════════════════════════════════════════════════════
        // AGENT COORDINATION - SINGLETON FOR PHYSICAL DEVICE
        // ══════════════════════════════════════════════════════════════
        // Single physical robot requires singleton coordination

        services.AddSingleton<IAgentManager, UnifiedAgentManager>();
        services.AddSingleton<IRuntimeAgentProvider>(provider =>
            new RuntimeAgentProvider(
                provider.GetRequiredService<IAgentManager>()));

        return services;
    }
}
```

---

### Best Practices

#### 1. When to Use Singleton

Use Singleton for services that:

- Expose `IObservable<T>` for GraphQL subscriptions
- Coordinate physical device access
- Manage per-execution state with proper cleanup
- Coordinate with other Singleton services

#### 2. Per-Execution State Management

Always follow this pattern for Singleton services with execution-specific state:

```csharp
public class MySingletonService
{
    // Singleton dependencies
    private readonly ILogger _logger;

    // Per-execution state (nullable)
    private Subject<string>? _eventStream;
    private IDisposable? _subscription;
    private volatile bool _isCleaningUp;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            // 1. CREATE per-execution state
            _eventStream = new Subject<string>();
            _isCleaningUp = false;

            // 2. USE the state
            _eventStream.OnNext("event");

            // 3. EXECUTE logic
            await DoWorkAsync(ct);
        }
        finally
        {
            // 4. CLEANUP (guaranteed)
            _isCleaningUp = true;
            _subscription?.Dispose();
            _eventStream?.Dispose();
            _eventStream = null;
        }
    }

    private void OnEvent(string eventData)
    {
        // Check cleanup flag before using Subject
        if (!_isCleaningUp && _eventStream != null)
        {
            _eventStream.OnNext(eventData);
        }
    }
}
```

#### 3. Change Tracker Event Wiring

Wire up orchestrators to change trackers in constructor:

```csharp
public class MyOrchestrator
{
    private readonly INodeChangeTracker _nodeChangeTracker;

    public event EventHandler<EntityChangedEventArgs<Node>>? NodeDataChanged;

    public MyOrchestrator(INodeChangeTracker nodeChangeTracker)
    {
        _nodeChangeTracker = nodeChangeTracker;

        // Wire up event to change tracker
        NodeDataChanged += (_, args) => _nodeChangeTracker.UpdateEntities(args.Entities);
    }

    public async Task<Node> CreateNodeAsync(Node node)
    {
        var createdNode = await _repository.CreateAsync(node);

        // Raise event to notify change tracker
        var allNodes = await _repository.GetAllAsync();
        NodeDataChanged?.Invoke(this, new EntityChangedEventArgs<Node>(allNodes));

        return createdNode;
    }
}
```

#### 4. GraphQL Subscription Registration

Always inject the orchestrator, not the change tracker:

```csharp
// CORRECT: Inject orchestrator (facade for change tracker)
public class Subscription
{
    [Subscribe]
    public IObservable<IReadOnlyList<Node>> NodesChanged(
        [Service] ICrudSchedulingOrchestrator orchestrator)
    {
        return orchestrator.NodesChanged;
    }
}

// AVOID: Direct change tracker injection
public class Subscription
{
    [Subscribe]
    public IObservable<IReadOnlyList<Node>> NodesChanged(
        [Service] INodeChangeTracker tracker)  // AVOID
    {
        return tracker.Nodes;
    }
}
```

**Why**: Orchestrator provides a facade that encapsulates the change tracker, allowing for future changes without
breaking GraphQL API.

#### 5. Cleanup Flag Pattern

Always use cleanup flag to prevent operations on disposed objects:

```csharp
private Subject<string>? _subject;
private volatile bool _isCleaningUp;

public void PublishEvent(string eventData)
{
    // Check flag before using Subject
    if (!_isCleaningUp && _subject != null)
    {
        _subject.OnNext(eventData);
    }
}

public void Cleanup()
{
    // Set flag BEFORE disposing
    _isCleaningUp = true;

    // Now safe to dispose
    _subject?.Dispose();
    _subject = null;
}
```

**Why**: Prevents `ObjectDisposedException` when events arrive during cleanup.

#### 6. Repository Injection — Avoiding Cache Bypass

`IRepository<T>` registrations are wrapped by `CachedRepository<T>` via `AddRepositoryCaching()`. Domain-specific
interfaces like `IProcedureRepository` are **NOT** cached.

```csharp
// CORRECT: Uses CachedRepository<Procedure> — writes update the cache
public class ProcedureOrchestrator(IRepository<Procedure> procedureRepository) { }

// CORRECT: Needs node/edge-specific methods, uses tracker pattern for consistency
public class CrudSchedulingOrchestrator(IProcedureRepository procedureRepository) { }

// WRONG: Uses raw repository for base CRUD — cache bypass bug!
public class ProcedureOrchestrator(IProcedureRepository procedureRepository) { }
```

**Rule:** If your service only calls base CRUD methods (`GetByIdAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync`,
`DeleteAsync`), inject `IRepository<T>`. Only inject `IProcedureRepository` when you need node/edge-specific methods.

Services that legitimately need `IProcedureRepository` for writes must use the **tracker pattern**: write via the raw
repository, then push fresh data to the change tracker so GraphQL subscribers see consistent state.

For full details, see
the [Infrastructure Layer — Cache Bypass Pitfall](../../Infrastructure/docs/README.md#cache-bypass-pitfall--iprocedurerepository).

#### 7. Thread Safety

Use thread-safe collections for per-execution state:

```csharp
// CORRECT: Thread-safe dictionary
private readonly ConcurrentDictionary<Guid, State> _states = new();

// AVOID: Non-thread-safe dictionary
private readonly Dictionary<Guid, State> _states = new();
```

**Why**: Singleton services can be called from multiple threads (GraphQL subscriptions, HTTP requests, background
tasks).

#### 7. Service Dependencies

Inject Singleton services into Singleton services:

```csharp
// CORRECT: Singleton → Singleton
services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();
services.AddSingleton<INodeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());

public class ExecutionOrchestrator
{
    public ExecutionOrchestrator(INodeChangeTracker tracker)  // OK
    {
    }
}

// AVOID: Singleton → Scoped (captive dependency)
services.AddSingleton<IMyService, MyService>();
services.AddScoped<IMyRepository, MyRepository>();

public class MyService
{
    public MyService(IMyRepository repository)  // WRONG!
    {
        // Singleton captures Scoped dependency
        // Repository instance lives forever, defeating Scoped lifetime
    }
}
```

**Why**: Singleton services capture Scoped dependencies, causing them to live forever (captive dependency anti-pattern).

---

### Summary

The `ApplicationServiceExtensions` class registers services with Singleton lifetime for specific technical reasons:

1. **GraphQL Subscriptions**: Require persistent BehaviorSubjects across HTTP requests
2. **Physical Device Coordination**: Single robot requires sequential execution enforcement
3. **Consecutive Execution Support**: Per-execution state pattern enables service reuse
4. **Change Tracking Consistency**: Entity services coordinate with Singleton change trackers

This design differs from typical ASP.NET Core patterns but is necessary given the application's constraints. The
per-execution state management pattern ensures Singleton services can support multiple consecutive executions safely.

**Key Takeaway**: Service lifetime is dictated by technical requirements, not convention. Always choose the lifetime
that best fits the use case, even if it differs from typical patterns.