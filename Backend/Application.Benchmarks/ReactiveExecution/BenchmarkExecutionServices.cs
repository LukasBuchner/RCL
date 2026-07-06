using System.Reactive.Concurrency;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.AgentCoordination;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Pipeline;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Expressions;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Builds a headless dependency-injection container that mirrors the execution-relevant
///     service graph of the GraphQL host, but without any GraphQL-, hosting-, or Postgres-layer
///     dependencies. The registrations here are hand-copied from
///     <c>ApplicationServiceExtensions.AddApplicationServices</c> and
///     <c>OrchestrationServiceExtensions.AddOrchestrationServices</c> in the GraphQL server,
///     restricted to the implementation types that live in <c>Application.csproj</c> (which itself
///     references only Agents, Domain, and Scheduling). The two GraphQL-layer registrations
///     (<c>IGraphQlMapperService</c> and <c>IDependencyEdgeValidator</c>) are intentionally dropped,
///     and the agent-synchronization, Postgres, and Windows-timer host services are not added.
/// </summary>
/// <remarks>
///     <para>
///         The benchmark feeds the container two seams: an in-memory <see cref="IProcedureRepository" />
///         (also bound to the closed <see cref="IRepository{Procedure}" /> the orchestrator's load path
///         reads) and a benchmark <see cref="IAgentManager" />, both supplied by the caller so each
///         iteration can build a fresh procedure and agent set. These overriding registrations are added
///         last so they win over the convention-based registration of the same service. The procedure
///         context is deliberately <i>not</i> mocked: the production <c>ProcedureContext</c> delegates to
///         <c>IProcedureOrchestrator</c>, so the benchmark loads its procedure through the real
///         <c>LoadProcedureAsync</c>, which also scopes the <c>ProcedureStateTracker</c> that backs the
///         runtime's node-change stream.
///     </para>
///     <para>
///         The concrete <see cref="ExecutionOrchestrator" /> is registered directly (not only behind
///         <see cref="IExecutionOrchestrator" />) because the benchmark awaits its
///         <c>CurrentExecution</c> task, which is exposed on the concrete class rather than the
///         interface. Resolving <see cref="IExecutionOrchestrator" /> from the built provider
///         validates that the replicated graph has no missing dependency.
///     </para>
/// </remarks>
public static class BenchmarkExecutionServices
{
    /// <summary>
    ///     Registers the execution-relevant Application services, the orchestration services, and the
    ///     benchmark-specific overrides (in-memory configuration, null logging, and the caller-supplied
    ///     repository, procedure context, and agent manager) into <paramref name="services" />.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="repo">
    ///     The in-memory procedure repository the execution runtime loads its procedure from. Registered
    ///     last so it overrides any convention-based <see cref="IProcedureRepository" /> (and the generic
    ///     <see cref="IRepository{Procedure}" /> the orchestrator's load path reads) registration.
    /// </param>
    /// <param name="agents">
    ///     The benchmark agent manager holding the mock <c>DummyRuntimeAgent</c> instances the
    ///     execution runtime resolves skills onto. Overrides the production <see cref="IAgentManager" />.
    /// </param>
    /// <returns>The same <paramref name="services" /> instance for chaining.</returns>
    public static IServiceCollection AddReactiveExecutionRuntime(
        this IServiceCollection services,
        IProcedureRepository repo,
        IAgentManager agents)
    {
        AddApplicationExecutionServices(services);
        AddOrchestrationExecutionServices(services);
        AddBenchmarkOverrides(services, repo, agents);

        return services;
    }

    /// <summary>
    ///     Replicates the execution-relevant registrations from
    ///     <c>ApplicationServiceExtensions.AddApplicationServices</c> (the GraphQL host), restricted to
    ///     implementation types resident in <c>Application.csproj</c>. The options binding
    ///     (<see cref="ExecutionPipelineConfiguration" /> and friends) is driven by the in-memory
    ///     <see cref="IConfiguration" /> added by <see cref="AddBenchmarkOverrides" />. The two
    ///     GraphQL-layer lines (<c>IGraphQlMapperService</c>, <c>IDependencyEdgeValidator</c>) are
    ///     deliberately omitted.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    private static void AddApplicationExecutionServices(IServiceCollection services)
    {
        // Options bound from the in-memory IConfiguration added in AddBenchmarkOverrides.
        services.AddOptions();

        // Procedure context for procedure-scoped operations. The production ProcedureContext delegates
        // CurrentProcedureId to IProcedureOrchestrator, so the benchmark loads its procedure through the
        // real IProcedureOrchestrator.LoadProcedureAsync rather than mocking the context directly. That
        // load path is also what drives the ProcedureStateTracker to scope its node/edge streams, so the
        // execution runtime's NodesChanged updates flow exactly as they do in production.
        services.AddSingleton<IProcedureContext, ProcedureContext>();

        // Procedure services - Singleton to support singleton entity services.
        services.AddSingleton<IRclProcedureQueryService, RclProcedureQueryService>();
        services.AddSingleton<IProcedureOrchestrator, ProcedureOrchestrator>();

        // Duration calculation services.
        services.AddSingleton<ITaskNodeDurationCalculator, TaskNodeDurationCalculator>();
        services.AddSingleton<IRouterNodeDurationCalculator, RouterNodeDurationCalculator>();

        // Agent coordination services for duration providers.
        services.AddSingleton<INodeAgentMapper, NodeAgentMapper>();
        services.AddSingleton<IAgentCapabilityAnalyzer, AgentCapabilityAnalyzer>();

        // Duration provider for planning mode (ExecutionAwareDurationProvider is created on demand).
        services.AddSingleton<PlanningModeDurationProvider>();

        // Timing aggregation service for scheduling pipeline.
        services.AddSingleton<ITimingAggregator, TimingAggregator>();

        // Hierarchical sorting service.
        services.AddSingleton<IHierarchicalSorter, HierarchicalSorter>();

        // Child node collection service.
        services.AddSingleton<IChildNodeCollector, ChildNodeCollector>();

        // Node timing and duration services.
        services.AddSingleton<INodeTimingMapper, NodeTimingMapper>();
        services.AddSingleton<INodeDurationAdjuster, NodeDurationAdjuster>();

        // Hierarchy and relationship mapping services.
        services.AddSingleton<IHierarchyValidator, HierarchyValidator>();
        services.AddSingleton<INodeRelationshipMapper, NodeRelationshipMapper>();
        services.AddSingleton<IScheduleResultConverter, ScheduleResultConverter>();

        // Statistics collection service.
        services.AddSingleton<ITimingStatisticsCollector, TimingStatisticsCollector>();

        // Scheduling orchestration services.
        services.AddSingleton<ITimingCalculationOrchestrator, TimingCalculationOrchestrator>();
        services.AddSingleton<INodeHierarchyProcessor, NodeHierarchyProcessor>();
        services.AddSingleton<INodeResolver, NodeResolver>();
        services.AddSingleton<ITimingCalculationEngine, TimingCalculationEngine>();

        // Scheduling pipeline support services (for refactored orchestrator).
        services.AddSingleton<IDurationProviderFactory, DurationProviderFactory>();
        services.AddSingleton<INodePositioningService, NodePositioningService>();
        services.AddSingleton<ITimingAnalyzer, TimingAnalyzer>();
        services.AddSingleton<ISchedulingPhaseLogger, SchedulingPhaseLogger>();

        // Supporting services for the CRUD orchestrator.
        services.AddSingleton<ISchedulingResultLogger, SchedulingResultLogger>();

        // Unified procedure state tracker — single BehaviorSubject for nodes, edges, and variables.
        services.AddSingleton<ProcedureStateTracker>();
        services.AddSingleton<IProcedureStateScope>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<INodeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IDependencyEdgeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IProcedureVariableChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());

        // CRUD scheduling orchestrator and extracted services.
        services.AddSingleton<ICrudDataPreparationService, CrudDataPreparationService>();
        services.AddSingleton<ICascadeDeletionService, CascadeDeletionService>();
        services.AddSingleton<ICrudNotificationService, CrudNotificationService>();
        services.AddSingleton<ICrudSchedulingOrchestrator, CrudSchedulingOrchestrator>();

        // Execution orchestrator for runtime procedure execution.
        services.AddSingleton<IExecutionEventDispatcher, ExecutionEventDispatcher>();
        services.AddSingleton<IExecutionPipelineBuilder, ExecutionPipelineBuilder>();
        // Agent name resolver — narrow facade used by the validator to look up display names.
        services.AddSingleton<IAgentNameResolver, AgentNameResolver>();
        // Agent serialization validator -- pure structural check, used by orchestrator as a hard gate.
        services.AddSingleton<IAgentSerializationValidator, AgentSerializationValidator>();
        // Reactive UX tracker -- wraps IAgentSerializationValidator in a throttled BehaviorSubject.
        services.AddSingleton<IProcedureValidationTracker, ProcedureValidationTracker>();
        // NOTE: the IExecutionOrchestrator registration is provided concretely in
        // AddBenchmarkOverrides so that ExecutionOrchestrator.CurrentExecution is reachable.

        // Execution event bus for event-driven execution.
        services.AddSingleton<ISkillExecutionEventBus, SkillExecutionEventBus>();

        // Execution dependency analysis for event-driven execution.
        services.AddSingleton<IDependencyGraphAnalyzer, DependencyGraphAnalyzer>();

        // Scene entity resolver — observable-backed cache for PositionTag/SceneObject freshness.
        services.AddSingleton<ISceneEntityResolver, SceneEntityResolver>();

        // Execution coordination for executing skills on agents.
        services.AddSingleton<ISkillExecutionCoordinator, SkillExecutionCoordinator>();

        // Execution trigger service for event-driven skill triggering.
        services.AddSingleton<IRouterBranchNavigator, RouterBranchNavigator>();
        services.AddSingleton<ISkillTriggerHandler, SkillTriggerHandler>();
        services.AddSingleton<IRouterTriggerHandler, RouterTriggerHandler>();
        services.AddSingleton<IExecutionTriggerService, ExecutionTriggerService>();

        // Router evaluation service for conditional branching during execution.
        services.AddSingleton<IRouterEvaluationService, RouterEvaluationService>();

        // Execution support services.
        services.AddSingleton<IExecutionIdAssigner, ExecutionIdAssigner>();
        services.AddSingleton<ISkillExecutionStateManager, SkillExecutionStateManager>();
        services.AddSingleton<IExecutionStateTransitionService, ExecutionStateTransitionService>();
        services.AddSingleton<IExecutionEventPublisher, ExecutionEventPublisher>();
        services.AddSingleton<IExecutionProgressMonitor, ExecutionProgressMonitor>();
        services.AddSingleton<IExecutionTimingPublisher, ExecutionTimingPublisher>();
        services.AddSingleton<IExecutionInitializer, ExecutionInitializer>();

        // Observability-only overrun monitor + its advisory side channel (causally isolated).
        // NOTE: registered as a plain singleton only; the hosted-service registration from the
        // GraphQL host is intentionally not replicated (no IHost runs in the benchmark).
        services.AddSingleton<IExecutionAdvisoryPublisher, ExecutionAdvisoryPublisher>();
        services.AddSingleton<AdaptiveSkillDurationOverrunMonitor>();

        // Re-scheduling services (extracted from ExecutionOrchestrator for SRP/DRY).
        services.AddSingleton<IExecutionProgressDataBuilder, ExecutionProgressDataBuilder>();
        services.AddSingleton<IExecutionTimeCalculator, ExecutionTimeCalculator>();
        services.AddSingleton<IReschedulingCoordinator, ReschedulingCoordinator>();

        // Variable management services for runtime variable handling.
        services.AddSingleton<IVariableResolver, VariableResolver>();
        services.AddSingleton<IProcedureVariableService, ProcedureVariableService>();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddSingleton<IBranchSelector, BranchSelector>();
        services.AddSingleton<IPropertyBindingService, PropertyBindingService>();

        // Router branch filtering for timeline display.
        services.AddSingleton<IRouterBranchFilterService, RouterBranchFilterService>();

        // Node hiding service for UI visibility management.
        services.AddSingleton<INodeHidingService, NodeHidingService>();

        // Position calculation services for node layout.
        services.AddSingleton<INodePositionXCalculator, NodePositionXCalculator>();
        services.AddSingleton<INodePositionYCalculator, NodePositionYCalculator>();
        services.AddSingleton<INodeHeightCalculator, NodeHeightCalculator>();
        services.AddSingleton<INodeWidthCalculator, NodeWidthCalculator>();

        // Entity management services.
        services.AddSingleton<INodeApplicationService, NodeApplicationService>();
        services.AddSingleton<IDependencyEdgeApplicationService, DependencyEdgeApplicationService>();
        services.AddSingleton<IAgentApplicationService, AgentApplicationService>();
        services.AddSingleton<ISkillApplicationService, SkillApplicationService>();
        services.AddSingleton<IPositionTagApplicationService, PositionTagApplicationService>();
        services.AddSingleton<ISceneObjectApplicationService, SceneObjectApplicationService>();

        // Runtime agent provider service - delegates to IAgentManager (the benchmark fake).
        services.AddSingleton<IRuntimeAgentProvider>(provider =>
            new RuntimeAgentProvider(
                provider.GetRequiredService<IAgentManager>(),
                provider.GetRequiredService<ILogger<RuntimeAgentProvider>>()));

        // GraphQL-layer registrations intentionally dropped (types live in GraphQLServer):
        //   services.AddSingleton<IGraphQlMapperService, GraphQlMapperService>();
        //   services.AddSingleton<IDependencyEdgeValidator, DependencyEdgeValidator>();
    }

    /// <summary>
    ///     Replicates the orchestration registrations from
    ///     <c>OrchestrationServiceExtensions.AddOrchestrationServices</c>, minus the
    ///     <c>WindowsTimerResolutionService</c> hosted service (no <c>IHost</c> runs in the benchmark).
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    private static void AddOrchestrationExecutionServices(IServiceCollection services)
    {
        // Graph building services.
        services.AddSingleton<IEdgeTypeMapper, EdgeTypeMapper>();
        services.AddSingleton<IExecutionGraphBuilder, ExecutionGraphBuilder>();

        // Schedule planning service.
        services.AddSingleton<ISchedulePlanner, SchedulePlanner>();

        // Rx scheduler and clock — real wall-clock execution.
        services.AddSingleton<IScheduler>(_ => Scheduler.Default);
        services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

        // WindowsTimerResolutionService hosted service intentionally not registered here.
    }

    /// <summary>
    ///     Adds the benchmark-specific overrides last so they win over the convention-based
    ///     registrations above: an in-memory <see cref="IConfiguration" /> with small
    ///     <c>ExecutionPipeline</c> publish intervals (so few reschedule snapshots collapse), null
    ///     logging, the caller-supplied repository/context/agent-manager seams, and the concrete
    ///     <see cref="ExecutionOrchestrator" /> registration that exposes <c>CurrentExecution</c>.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="repo">The in-memory procedure repository.</param>
    /// <param name="agents">The benchmark agent manager.</param>
    private static void AddBenchmarkOverrides(
        IServiceCollection services,
        IProcedureRepository repo,
        IAgentManager agents)
    {
        // In-memory configuration with small ExecutionPipeline publish/sample intervals so that
        // rapid reschedules are not collapsed into a few snapshots (keeps the NodesChanged proxy
        // count as close as possible to the real reschedule cadence).
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExecutionPipeline:RescheduleSampleIntervalMs"] = "1",
                ["ExecutionPipeline:AgentPublishIntervalMs"] = "1",
                ["ExecutionPipeline:FrontendPublishIntervalMs"] = "1",
                ["ExecutionPipeline:TimingPublishIntervalMs"] = "1"
            })
            .Build();
        services.AddSingleton(configuration);

        // Bind the execution-pipeline options from the in-memory configuration above.
        services.Configure<ExecutionPipelineConfiguration>(
            configuration.GetSection("ExecutionPipeline"));
        services.Configure<SchedulingConfiguration>(configuration.GetSection("Scheduling"));
        services.Configure<AdaptiveSkillMonitoringConfiguration>(
            configuration.GetSection("AdaptiveSkillMonitoring"));

        // Null logging — no logger output during the benchmark.
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        // Caller-supplied seams — registered last so they override the production registrations.
        // The parameters are statically typed as the interfaces, so generic type inference keys
        // each AddSingleton(instance) by the interface (IProcedureRepository / IAgentManager) — the
        // type every consumer injects — not by the runtime fake type. The procedure context is left as
        // the production ProcedureContext (registered above), which delegates to IProcedureOrchestrator,
        // so loading through the orchestrator is what makes the current procedure visible.
        services.AddSingleton(repo);
        services.AddSingleton(agents);
        // IRuntimeAgentProvider is registered above as a factory over IAgentManager, so the
        // benchmark agent manager flows through to skill resolution without a separate override.

        // Generic in-memory repository safety net. The Infrastructure/Postgres layer that supplies the
        // real IRepository<T> is excluded, yet replicated entity-management application services declare
        // an IRepository<T> dependency. Registering the open generic lets any such service the execution
        // graph activates resolve a no-op repository instead of failing at provider-resolution time.
        services.AddSingleton(typeof(IRepository<>), typeof(BenchmarkRepository<>));

        // The procedure aggregate is the one IRepository<T> the load path must read for real:
        // ProcedureOrchestrator.LoadProcedureAsync calls IRepository<Procedure>.GetByIdAsync/UpdateAsync.
        // Bind that closed generic to the same in-memory procedure repository (added after the open
        // generic so it wins for IRepository<Procedure>) so the orchestrator loads the seeded procedure
        // instead of the empty safety-net repository. IProcedureRepository extends IRepository<Procedure>,
        // so the same instance serves both.
        services.AddSingleton<IRepository<Procedure>>(repo);

        // The production AgentApplicationService (registered above) is reached by NodeAgentMapper during
        // node-to-agent mapping, but it would read agents from the empty safety-net repository. Override
        // it with a fake backed by the benchmark agent manager so the mapping sees the real mock agents.
        services.AddSingleton<IAgentApplicationService>(sp =>
            new BenchmarkAgentApplicationService(sp.GetRequiredService<IAgentManager>()));

        // Concrete orchestrator: ExecutionOrchestrator.CurrentExecution is on the concrete class,
        // not the interface, so the benchmark must resolve the concrete type to await the run.
        services.AddSingleton<ExecutionOrchestrator>();
        services.AddSingleton<IExecutionOrchestrator>(sp => sp.GetRequiredService<ExecutionOrchestrator>());
    }
}