using FHOOE.Freydis.Agents.Services;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.AgentCoordination;
using FHOOE.Freydis.Application.Services.AgentCoordination.Registration;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillSynchronization;
using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Application.Services.Common;
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
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Services.Validation;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    ///     Adds core application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure scheduling options from application settings
        services.Configure<SchedulingConfiguration>(configuration.GetSection("Scheduling"));

        // Configure execution pipeline sampling intervals
        services.Configure<ExecutionPipelineConfiguration>(configuration.GetSection("ExecutionPipeline"));

        // Configure the observability-only adaptive-skill overrun monitor
        services.Configure<AdaptiveSkillMonitoringConfiguration>(
            configuration.GetSection("AdaptiveSkillMonitoring"));

        // Procedure context for procedure-scoped operations
        services.AddSingleton<IProcedureContext, ProcedureContext>();

        // Procedure services - Singleton to support singleton entity services
        services.AddSingleton<IRclProcedureQueryService, RclProcedureQueryService>();
        services.AddSingleton<IProcedureOrchestrator, ProcedureOrchestrator>();

        // Duration calculation services
        services.AddSingleton<ITaskNodeDurationCalculator, TaskNodeDurationCalculator>();
        services.AddSingleton<IRouterNodeDurationCalculator, RouterNodeDurationCalculator>();

        // Agent coordination services for duration providers
        services.AddSingleton<INodeAgentMapper, NodeAgentMapper>();
        services.AddSingleton<IAgentCapabilityAnalyzer, AgentCapabilityAnalyzer>();

        // Duration provider for planning mode (ExecutionAwareDurationProvider is created on demand)
        services.AddSingleton<PlanningModeDurationProvider>();

        // Timing aggregation service for scheduling pipeline
        services.AddSingleton<ITimingAggregator, TimingAggregator>();

        // Hierarchical sorting service
        services.AddSingleton<IHierarchicalSorter, HierarchicalSorter>();

        // Child node collection service
        services.AddSingleton<IChildNodeCollector, ChildNodeCollector>();

        // Node timing and duration services
        services.AddSingleton<INodeTimingMapper, NodeTimingMapper>();
        services.AddSingleton<INodeDurationAdjuster, NodeDurationAdjuster>();

        // Hierarchy and relationship mapping services
        services.AddSingleton<IHierarchyValidator, HierarchyValidator>();
        services.AddSingleton<INodeRelationshipMapper, NodeRelationshipMapper>();
        services.AddSingleton<IScheduleResultConverter, ScheduleResultConverter>();

        // Statistics collection service
        services.AddSingleton<ITimingStatisticsCollector, TimingStatisticsCollector>();

        // Scheduling orchestration services
        services.AddSingleton<ITimingCalculationOrchestrator, TimingCalculationOrchestrator>();
        services.AddSingleton<INodeHierarchyProcessor, NodeHierarchyProcessor>();
        services.AddSingleton<INodeResolver, NodeResolver>();
        services.AddSingleton<ITimingCalculationEngine, TimingCalculationEngine>();

        // Scheduling pipeline support services (for refactored orchestrator)
        services.AddSingleton<IDurationProviderFactory, DurationProviderFactory>();
        services.AddSingleton<INodePositioningService, NodePositioningService>();
        services.AddSingleton<ITimingAnalyzer, TimingAnalyzer>();
        services.AddSingleton<ISchedulingPhaseLogger, SchedulingPhaseLogger>();

        // Supporting services for the CRUD orchestrator
        services.AddSingleton<ISchedulingResultLogger, SchedulingResultLogger>();

        // Unified procedure state tracker — single BehaviorSubject for nodes, edges, and variables.
        // Data is scoped to the currently loaded procedure: ProcedureOrchestrator drives load and
        // unload events via IProcedureStateScope so that no cross-procedure data can ever leak into
        // the observable streams.
        services.AddSingleton<ProcedureStateTracker>();
        services.AddSingleton<IProcedureStateScope>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<INodeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IDependencyEdgeChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());
        services.AddSingleton<IProcedureVariableChangeTracker>(sp => sp.GetRequiredService<ProcedureStateTracker>());

        // CRUD scheduling orchestrator and extracted services
        services.AddSingleton<ICrudDataPreparationService, CrudDataPreparationService>();
        services.AddSingleton<ICascadeDeletionService, CascadeDeletionService>();
        services.AddSingleton<ICrudNotificationService, CrudNotificationService>();
        services.AddSingleton<ICrudSchedulingOrchestrator, CrudSchedulingOrchestrator>();

        // Execution orchestrator for runtime procedure execution
        // IMPORTANT: Singleton - only one execution can run at a time
        services.AddSingleton<IExecutionEventDispatcher, ExecutionEventDispatcher>();
        services.AddSingleton<IExecutionPipelineBuilder, ExecutionPipelineBuilder>();
        // Agent name resolver — narrow facade used by the validator to look up display names
        services.AddSingleton<IAgentNameResolver, AgentNameResolver>();
        // Agent serialization validator -- pure structural check, used by orchestrator as a hard gate
        services.AddSingleton<IAgentSerializationValidator, AgentSerializationValidator>();
        // Reactive UX tracker -- wraps IAgentSerializationValidator in a throttled BehaviorSubject for the GraphQL subscription
        services.AddSingleton<IProcedureValidationTracker, ProcedureValidationTracker>();
        services.AddSingleton<IExecutionOrchestrator, ExecutionOrchestrator>();

        // Execution event bus for event-driven execution
        services.AddSingleton<ISkillExecutionEventBus, SkillExecutionEventBus>();

        // Execution dependency analysis for event-driven execution
        services.AddSingleton<IDependencyGraphAnalyzer, DependencyGraphAnalyzer>();

        // Scene entity resolver — observable-backed cache for PositionTag/SceneObject freshness
        services.AddSingleton<ISceneEntityResolver, SceneEntityResolver>();

        // Execution coordination for executing skills on agents
        services.AddSingleton<ISkillExecutionCoordinator, SkillExecutionCoordinator>();

        // Execution trigger service for event-driven skill triggering
        services.AddSingleton<IRouterBranchNavigator, RouterBranchNavigator>();
        services.AddSingleton<ISkillTriggerHandler, SkillTriggerHandler>();
        services.AddSingleton<IRouterTriggerHandler, RouterTriggerHandler>();
        services.AddSingleton<IExecutionTriggerService, ExecutionTriggerService>();

        // Router evaluation service for conditional branching during execution
        services.AddSingleton<IRouterEvaluationService, RouterEvaluationService>();

        // Execution support services
        services.AddSingleton<IExecutionIdAssigner, ExecutionIdAssigner>();
        services.AddSingleton<ISkillExecutionStateManager, SkillExecutionStateManager>();
        services.AddSingleton<IExecutionStateTransitionService, ExecutionStateTransitionService>();
        services.AddSingleton<IExecutionEventPublisher, ExecutionEventPublisher>();
        services.AddSingleton<IExecutionProgressMonitor, ExecutionProgressMonitor>();
        services.AddSingleton<IExecutionTimingPublisher, ExecutionTimingPublisher>();
        services.AddSingleton<IExecutionInitializer, ExecutionInitializer>();

        // Observability-only overrun monitor + its advisory side channel (causally isolated from the bus)
        services.AddSingleton<IExecutionAdvisoryPublisher, ExecutionAdvisoryPublisher>();
        services.AddSingleton<AdaptiveSkillDurationOverrunMonitor>();
        services.AddHostedService(sp => sp.GetRequiredService<AdaptiveSkillDurationOverrunMonitor>());

        // Re-scheduling services (extracted from ExecutionOrchestrator for SRP/DRY)
        services.AddSingleton<IExecutionProgressDataBuilder, ExecutionProgressDataBuilder>();
        services.AddSingleton<IExecutionTimeCalculator, ExecutionTimeCalculator>();
        services.AddSingleton<IReschedulingCoordinator, ReschedulingCoordinator>();

        // Variable management services for runtime variable handling
        // These services are stateless and can be Singleton - they operate on VariableContext passed as parameters
        services.AddSingleton<IVariableResolver, VariableResolver>();
        services.AddSingleton<IProcedureVariableService, ProcedureVariableService>();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddSingleton<IBranchSelector, BranchSelector>();
        services.AddSingleton<IPropertyBindingService,
            PropertyBindingService>();

        // Router branch filtering for timeline display
        services.AddSingleton<IRouterBranchFilterService, RouterBranchFilterService>();

        // Node hiding service for UI visibility management
        services.AddSingleton<INodeHidingService, NodeHidingService>();

        // Position calculation services for node layout
        services.AddSingleton<INodePositionXCalculator,
            NodePositionXCalculator>();
        services.AddSingleton<INodePositionYCalculator,
            NodePositionYCalculator>();
        services.AddSingleton<INodeHeightCalculator,
            NodeHeightCalculator>();
        services.AddSingleton<INodeWidthCalculator,
            NodeWidthCalculator>();

        // Entity management services
        services.AddSingleton<INodeApplicationService, NodeApplicationService>();
        services.AddSingleton<IDependencyEdgeApplicationService, DependencyEdgeApplicationService>();
        services.AddSingleton<IAgentApplicationService, AgentApplicationService>();
        services.AddSingleton<ISkillApplicationService, SkillApplicationService>();
        services.AddSingleton<IPositionTagApplicationService, PositionTagApplicationService>();
        services.AddSingleton<ISceneObjectApplicationService, SceneObjectApplicationService>();

        // Runtime agent provider service - delegates to IAgentManager from AgentServiceExtensions
        services.AddSingleton<IRuntimeAgentProvider>(provider =>
            new RuntimeAgentProvider(
                provider.GetRequiredService<IAgentManager>(),
                provider.GetRequiredService<ILogger<RuntimeAgentProvider>>()));

        // GraphQL layer mapper service
        services.AddSingleton<IGraphQlMapperService, GraphQlMapperService>();

        // Dependency edge validation service
        services.AddSingleton<IDependencyEdgeValidator, DependencyEdgeValidator>();


        return services;
    }

    /// <summary>
    ///     Adds agent discovery and registration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentSynchronizationServices(this IServiceCollection services)
    {
        // Scene entity provider for agent creation
        services.AddSingleton<ISceneEntityProvider, SceneEntityProvider>();

        // Agent registration and skill synchronization
        services.AddSingleton<IAgentRegistrationService, AgentRegistrationService>();
        services.AddSingleton<ISkillSynchronizationService, SkillSynchronizationService>();

        // Lifecycle notifier bridges dynamic agent connections (e.g. Digital Twin WebSocket)
        // to the persistent domain model so they appear in GraphQL queries
        services.AddSingleton<IAgentLifecycleNotifier, DomainAgentLifecycleNotifier>();

        return services;
    }
}