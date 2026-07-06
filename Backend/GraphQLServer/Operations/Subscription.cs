using System.Globalization;
using System.Reactive.Linq;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.GraphQLServer.Types;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.GraphQLServer.Operations;

/// <summary>
///     GraphQL subscription operations for real-time data updates.
/// </summary>
/// <remarks>
///     This class defines all GraphQL subscription endpoints that allow clients to receive
///     real-time updates when data changes in the system. It uses HotChocolate's subscription
///     system with Rx.NET observables for reactive streaming.
/// </remarks>
public partial class Subscription
{
    /// <summary>
    ///     Subscription endpoint that notifies clients when nodes have changed.
    /// </summary>
    /// <param name="nodes">The current list of all nodes after the change event.</param>
    /// <returns>The list of nodes to be sent to the subscribing client.</returns>
    /// <remarks>
    ///     This method is invoked by HotChocolate when node changes occur.
    ///     The actual subscription logic is handled by the SubscribeToNodesChanged method.
    /// </remarks>
    [Subscribe(With = nameof(SubscribeToNodesChanged))]
    public List<Node> NodesChanged([EventMessage] List<Node> nodes, [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionNodeschangedCalledWithNodecountNodes(logger, nodes.Count);

        if (!logger.IsEnabled(LogLevel.Debug)) return nodes;

        foreach (var node in nodes)
            switch (node)
            {
                case SkillExecutionNode skillNode:
                    // Use structured SkillTimingLogger for consistency across the pipeline
                    var skillInfo = string.Create(CultureInfo.InvariantCulture,
                        $"Progress={skillNode.SkillExecutionTask.Progress:F2}%, ParentId={skillNode.ParentId}, Position=({skillNode.Position.X:F2}, {skillNode.Position.Y:F2}), Height={skillNode.Height:F2}, Width={skillNode.Width:F2}, Extent={skillNode.Extent ?? "null"}, Hidden={skillNode.Hidden}");
                    logger.LogSkillTiming(
                        "SUBSCRIPTION_OUTPUT",
                        skillNode.Id,
                        skillNode.SkillExecutionTask.Skill.Name,
                        skillNode.SkillExecutionTask.AgentId,
                        skillNode.SkillExecutionTask.ExecutionId,
                        skillNode.SkillExecutionTask.IsExecuting == true ? "EXECUTING" : "IDLE",
                        false, // We don't have this info at this level
                        skillNode.SkillExecutionTask.StartTime,
                        skillNode.SkillExecutionTask.FinishTime,
                        skillNode.SkillExecutionTask.Duration,
                        additionalInfo: skillInfo);
                    break;
                case TaskNode taskNode:
                    LogGraphqlSubscriptionTasknodeWithParentAndExtent(logger,
                        taskNode.Id,
                        taskNode.Task.Name,
                        taskNode.Task.StartTime,
                        taskNode.Task.Duration,
                        taskNode.ParentId,
                        taskNode.Position.X,
                        taskNode.Position.Y,
                        taskNode.Height,
                        taskNode.Width,
                        taskNode.Extent ?? "null",
                        taskNode.Hidden);
                    break;
                case RouterNode routerNode:
                    LogGraphqlSubscriptionRouternodeWithParent(logger,
                        routerNode.Id,
                        routerNode.RouterTask.Name,
                        routerNode.RouterTask.StartTime,
                        routerNode.RouterTask.Duration,
                        routerNode.ParentId,
                        routerNode.Position.X,
                        routerNode.Position.Y,
                        routerNode.Height,
                        routerNode.Width,
                        routerNode.RouterTask.Branches?.Count ?? 0,
                        routerNode.RouterTask.SelectedBranchTargetNodeId,
                        routerNode.Hidden);
                    break;
                default:
                    LogGraphqlSubscriptionNodeIdNodeidTypeNodetype(logger, node.Id, node.GetType().Name, node.Hidden);
                    break;
            }

        return nodes;
    }

    /// <summary>
    ///     Creates an observable subscription for node changes using the simplified NodeApplicationService.
    /// </summary>
    /// <param name="nodeService">The node application service that provides the reactive stream of node changes.</param>
    /// <returns>An observable sequence that emits lists of nodes whenever changes occur.</returns>
    /// <remarks>
    ///     This method sets up the subscription pipeline for node changes.
    ///     It connects directly to the NodeApplicationService's Nodes observable,
    ///     which uses integrated reactive notifications instead of the previous event dispatcher pattern.
    ///     The conversion from IReadOnlyList to List is necessary for GraphQL serialization.
    /// </remarks>
    public static IObservable<List<Node>> SubscribeToNodesChanged(
        [Service] INodeApplicationService nodeService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetonodeschangedCalledSettingUpNodesSubscription(logger);
        return nodeService.Nodes
            .Do(nodes => LogGraphqlSubscriptionNodeschangedObservableEmittedNodecountNodes(logger, nodes.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when dependency edges have changed.
    /// </summary>
    /// <param name="edges">The current list of all dependency edges after the change event.</param>
    /// <returns>The list of dependency edges to be sent to the subscribing client.</returns>
    /// <remarks>
    ///     This method is invoked by HotChocolate when dependency edge changes occur.
    ///     The actual subscription logic is handled by the SubscribeToDependencyEdgesChanged method.
    /// </remarks>
    [Subscribe(With = nameof(SubscribeToDependencyEdgesChanged))]
    public List<DependencyEdge> DependencyEdgesChanged([EventMessage] List<DependencyEdge> edges,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionDependencyedgeschangedCalledWithEdgecountEdges(logger, edges.Count);

        if (!logger.IsEnabled(LogLevel.Trace)) return edges;

        foreach (var edge in edges)
            LogGraphqlSubscriptionDependencyedgeIdEdgeidSourceSourceidSourcehandleTarget(logger, edge.Id, edge.SourceId,
                edge.SourceHandle ?? "", edge.TargetId, edge.TargetHandle ?? "");

        return edges;
    }

    /// <summary>
    ///     Creates an observable subscription for dependency edge changes using the simplified
    ///     DependencyEdgeApplicationService.
    /// </summary>
    /// <param name="edgeService">The dependency edge application service that provides the reactive stream of edge changes.</param>
    /// <returns>An observable sequence that emits lists of dependency edges whenever changes occur.</returns>
    /// <remarks>
    ///     This method sets up the subscription pipeline for dependency edge changes.
    ///     It connects directly to the DependencyEdgeApplicationService's DependencyEdges observable,
    ///     which uses integrated reactive notifications instead of the previous event dispatcher pattern.
    /// </remarks>
    public static IObservable<List<DependencyEdge>> SubscribeToDependencyEdgesChanged(
        [Service] IDependencyEdgeApplicationService edgeService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetodependencyedgeschangedCalledSettingUpDependencyEdges(logger);
        return edgeService.DependencyEdges
            .Do(edges =>
                LogGraphqlSubscriptionDependencyedgeschangedObservableEmittedEdgecountEdges(logger, edges.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when agents have changed.
    /// </summary>
    /// <param name="agents">The current list of all agents after the change event.</param>
    /// <returns>The list of agents to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToAgentsChanged))]
    public List<Agent> AgentsChanged([EventMessage] List<Agent> agents, [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionAgentschangedCalledWithAgentcountAgents(logger, agents.Count);

        if (!logger.IsEnabled(LogLevel.Trace)) return agents;

        foreach (var agent in agents)
            LogGraphqlSubscriptionAgentIdAgentidNameAgentnameStateStateSkillcount(logger, agent.Id, agent.Name,
                agent.State, agent.SkillIds?.Count ?? 0,
                agent.LastSeenUtc?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "Never");

        return agents;
    }

    /// <summary>
    ///     Creates an observable subscription for agent changes using the simplified AgentApplicationService.
    /// </summary>
    /// <param name="agentService">The agent application service that provides the reactive stream of agent changes.</param>
    /// <returns>An observable sequence that emits lists of agents whenever changes occur.</returns>
    public static IObservable<List<Agent>> SubscribeToAgentsChanged(
        [Service] IAgentApplicationService agentService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetoagentschangedCalledSettingUpAgentsSubscription(logger);
        return agentService.OnAgentsChanged()
            .Do(agents => LogGraphqlSubscriptionAgentschangedObservableEmittedAgentcountAgents(logger, agents.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when skills have changed.
    /// </summary>
    /// <param name="skills">The current list of all skills after the change event.</param>
    /// <returns>The list of skills to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToSkillsChanged))]
    public List<Skill> SkillsChanged([EventMessage] List<Skill> skills, [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSkillschangedCalledWithSkillcountSkills(logger, skills.Count);

        if (!logger.IsEnabled(LogLevel.Trace)) return skills;

        foreach (var skill in skills)
            LogGraphqlSubscriptionSkillIdSkillidNameSkillnameDescriptionDescription(logger, skill.Id, skill.Name,
                skill.Description, skill.Properties?.Count ?? 0);

        return skills;
    }

    /// <summary>
    ///     Creates an observable subscription for skill changes using the simplified SkillApplicationService.
    /// </summary>
    /// <param name="skillService">The skill application service that provides the reactive stream of skill changes.</param>
    /// <returns>An observable sequence that emits lists of skills whenever changes occur.</returns>
    public static IObservable<List<Skill>> SubscribeToSkillsChanged(
        [Service] ISkillApplicationService skillService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetoskillschangedCalledSettingUpSkillsSubscription(logger);
        return skillService.OnSkillsChanged()
            .Do(skills => LogGraphqlSubscriptionSkillschangedObservableEmittedSkillcountSkills(logger, skills.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when position tags have changed.
    /// </summary>
    /// <param name="positionTags">The current list of all position tags after the change event.</param>
    /// <returns>The list of position tags to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToPositionTagsChanged))]
    public List<PositionTag> PositionTagsChanged([EventMessage] List<PositionTag> positionTags,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionPositiontagschangedCalledWithPositiontagcountPositionTags(logger, positionTags.Count);

        if (!logger.IsEnabled(LogLevel.Trace)) return positionTags;

        foreach (var positionTag in positionTags)
            LogGraphqlSubscriptionPositiontagIdPositiontagidTagTagPositionXy(logger, positionTag.Id, positionTag.Tag,
                positionTag.Position.X, positionTag.Position.Y);

        return positionTags;
    }

    /// <summary>
    ///     Creates an observable subscription for position tag changes using the simplified PositionTagApplicationService.
    /// </summary>
    /// <param name="positionTagService">
    ///     The position tag application service that provides the reactive stream of position tag
    ///     changes.
    /// </param>
    /// <returns>An observable sequence that emits lists of position tags whenever changes occur.</returns>
    public static IObservable<List<PositionTag>> SubscribeToPositionTagsChanged(
        [Service] IPositionTagApplicationService positionTagService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetopositiontagschangedCalledSettingUpPositionTagsSubscription(logger);
        return positionTagService.OnPositionTagsChanged()
            .Do(positionTags =>
                LogGraphqlSubscriptionPositiontagschangedObservableEmittedPositiontagcountPositionTags(logger,
                    positionTags.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when scene objects have changed.
    /// </summary>
    /// <param name="sceneObjects">The current list of all scene objects after the change event.</param>
    /// <returns>The list of scene objects to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToSceneObjectsChanged))]
    public List<SceneObject> SceneObjectsChanged([EventMessage] List<SceneObject> sceneObjects,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSceneobjectschangedCalledWithSceneobjectcountSceneObjects(logger, sceneObjects.Count);

        if (!logger.IsEnabled(LogLevel.Trace)) return sceneObjects;

        foreach (var sceneObject in sceneObjects)
            LogGraphqlSubscriptionSceneobjectIdSceneobjectidNameNamePositionXy(logger, sceneObject.Id, sceneObject.Name,
                sceneObject.Position.X, sceneObject.Position.Y);

        return sceneObjects;
    }

    /// <summary>
    ///     Creates an observable subscription for scene object changes using the simplified SceneObjectApplicationService.
    /// </summary>
    /// <param name="sceneObjectService">
    ///     The scene object application service that provides the reactive stream of scene object
    ///     changes.
    /// </param>
    /// <returns>An observable sequence that emits lists of scene objects whenever changes occur.</returns>
    public static IObservable<List<SceneObject>> SubscribeToSceneObjectsChanged(
        [Service] ISceneObjectApplicationService sceneObjectService,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetosceneobjectschangedCalledSettingUpSceneObjectsSubscription(logger);
        return sceneObjectService.OnSceneObjectsChanged()
            .Do(sceneObjects =>
                LogGraphqlSubscriptionSceneobjectschangedObservableEmittedSceneobjectcountSceneObjects(logger,
                    sceneObjects.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when execution events occur.
    /// </summary>
    /// <param name="executionEvent">The execution event (Start or Finish).</param>
    /// <returns>The execution event to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToExecutionEvents))]
    public ExecutionEventDto ExecutionEvents([EventMessage] ExecutionEventDto executionEvent,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionExecutioneventsCalledWithEventtypeEventForSkillSkillid(logger, executionEvent.EventType,
            executionEvent.SkillId);

        if (logger.IsEnabled(LogLevel.Debug))
            LogGraphqlSubscriptionExecutioneventSkillSkillidEventEventtypeTimestampTimestamp(logger,
                executionEvent.SkillId,
                executionEvent.EventType,
                executionEvent.Timestamp);

        return executionEvent;
    }

    /// <summary>
    ///     Creates an observable subscription for execution events using the SkillExecutionEventBus.
    /// </summary>
    /// <param name="eventBus">The skill execution event bus that provides the reactive stream of execution events.</param>
    /// <returns>An observable sequence that emits execution events whenever they occur.</returns>
    public static IObservable<ExecutionEventDto> SubscribeToExecutionEvents(
        [Service] ISkillExecutionEventBus eventBus,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetoexecutioneventsCalledSettingUpExecutionEvents(logger);
        return eventBus.AllEvents
            .Do(evt => LogGraphqlSubscriptionExecutioneventsObservableEmittedEventtypeEventForSkillSkillid(logger,
                evt.EventType, evt.SkillId))
            .Select(ExecutionEventDto.FromExecutionEvent);
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when execution events of a specific type occur.
    /// </summary>
    /// <param name="executionEvent">The execution event (Start or Finish).</param>
    /// <param name="eventType">Optional filter for event type (Start or Finish).</param>
    /// <returns>The execution event to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToExecutionEventsByType))]
    public ExecutionEventDto ExecutionEventsByType(
        [EventMessage] ExecutionEventDto executionEvent,
        ExecutionEventType? eventType,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionExecutioneventsbytypeCalledWithEventtypeEventForSkillSkillidFilterFilter(logger,
            executionEvent.EventType,
            executionEvent.SkillId,
            eventType?.ToString() ?? "None");

        return executionEvent;
    }

    /// <summary>
    ///     Creates an observable subscription for execution events filtered by type.
    /// </summary>
    /// <param name="eventType">Optional filter for event type (Start or Finish).</param>
    /// <param name="eventBus">The skill execution event bus that provides the reactive stream of execution events.</param>
    /// <returns>An observable sequence that emits execution events of the specified type.</returns>
    public static IObservable<ExecutionEventDto> SubscribeToExecutionEventsByType(
        ExecutionEventType? eventType,
        [Service] ISkillExecutionEventBus eventBus,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetoexecutioneventsbytypeCalledWithFilterFilter(logger,
            eventType?.ToString() ?? "None");

        var observable = eventType switch
        {
            ExecutionEventType.Start => eventBus.StartEvents,
            ExecutionEventType.Finish => eventBus.FinishEvents,
            ExecutionEventType.Failed => eventBus.FailedEvents,
            _ => eventBus.AllEvents
        };

        return observable
            .Do(evt => LogGraphqlSubscriptionExecutioneventsbytypeObservableEmittedEventtypeEventForSkillSkillid(logger,
                evt.EventType, evt.SkillId))
            .Select(ExecutionEventDto.FromExecutionEvent);
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when execution timing updates are available.
    /// </summary>
    [Subscribe(With = nameof(SubscribeToExecutionTimingChanged))]
    public ExecutionTimingDto ExecutionTimingChanged(
        [EventMessage] ExecutionTimingDto timing,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionExecutiontimingchangedCalled(logger,
            timing.CurrentTimeSeconds, timing.ProgressPercentage, timing.IsRunning);
        return timing;
    }

    /// <summary>
    ///     Creates an observable subscription for execution timing updates,
    ///     sampled at the frontend publish interval to avoid flooding the client.
    /// </summary>
    public static IObservable<ExecutionTimingDto> SubscribeToExecutionTimingChanged(
        [Service] IExecutionTimingPublisher timingPublisher,
        [Service] IOptions<ExecutionPipelineConfiguration> pipelineOptions,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribetoexecutiontimingchangedCalled(logger);
        var interval = pipelineOptions.Value.FrontendPublishIntervalMs;
        return timingPublisher.TimingUpdates
            .Sample(TimeSpan.FromMilliseconds(interval))
            .Select(ExecutionTimingDto.FromExecutionTimingInfo);
    }

    /// <summary>
    ///     Subscription endpoint that delivers operator-facing execution advisories (e.g. a skill
    ///     running past its scheduled finish) to the frontend. Advisory only — it carries no
    ///     control-plane state change.
    /// </summary>
    /// <param name="advisory">The advisory delivered to the subscriber.</param>
    /// <returns>The advisory DTO.</returns>
    [Subscribe(With = nameof(SubscribeToExecutionAdvisoryRaised))]
    public ExecutionAdvisoryDto ExecutionAdvisoryRaised([EventMessage] ExecutionAdvisoryDto advisory)
    {
        return advisory;
    }

    /// <summary>
    ///     Creates the observable subscription for execution advisories.
    /// </summary>
    /// <param name="advisoryPublisher">The advisory side-channel publisher.</param>
    /// <returns>An observable of advisory DTOs.</returns>
    public static IObservable<ExecutionAdvisoryDto> SubscribeToExecutionAdvisoryRaised(
        [Service] IExecutionAdvisoryPublisher advisoryPublisher)
    {
        return advisoryPublisher.Advisories
            .Select(ExecutionAdvisoryDto.FromExecutionAdvisory);
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when the variable definitions
    ///     of the loaded procedure change, including additions, updates, and removals.
    ///     Emits an empty list when no procedure is loaded.
    /// </summary>
    /// <param name="variables">The current list of variable definitions for the loaded procedure.</param>
    /// <returns>The list of variable definitions to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToProcedureVariablesChanged))]
    public List<VariableDefinition> ProcedureVariablesChanged(
        [EventMessage] List<VariableDefinition> variables,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionProcedureVariablesChanged(logger, variables.Count);
        return variables;
    }

    /// <summary>
    ///     Creates an observable subscription for procedure variable changes using the
    ///     <see cref="IProcedureVariableChangeTracker" />.
    ///     Emits the current variable definitions whenever they change, or an empty list when unloaded.
    /// </summary>
    /// <param name="tracker">The procedure variable change tracker providing the reactive stream.</param>
    /// <param name="logger">Logger for subscription diagnostics.</param>
    /// <returns>An observable sequence that emits lists of variable definitions whenever they change.</returns>
    public static IObservable<List<VariableDefinition>> SubscribeToProcedureVariablesChanged(
        [Service] IProcedureVariableChangeTracker tracker,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribeToProcedureVariablesChanged(logger);
        return tracker.Variables
            .Do(vars => LogGraphqlSubscriptionProcedureVariablesEmitted(logger, vars.Count))
            .Select(readOnlyList => readOnlyList.ToList());
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when the composite procedure validation
    ///     result changes.  Emits a new <see cref="ProcedureValidationResult" /> whenever the
    ///     set of violations changes, throttled to at most once per second.
    /// </summary>
    /// <param name="result">
    ///     The latest procedure validation result, containing agent serialization violations
    ///     and any other structural constraint violations.
    /// </param>
    /// <returns>The validation result to be sent to the subscribing client.</returns>
    /// <remarks>
    ///     This subscription is <strong>UX-only and not safety-critical</strong>.  Results may be
    ///     up to 1 second stale relative to the current graph state.  The hard execution gate is
    ///     enforced synchronously inside <c>ExecutionOrchestrator</c>.
    /// </remarks>
    [Subscribe(With = nameof(SubscribeToProcedureValidationChanged))]
    public ProcedureValidationResult ProcedureValidationChanged(
        [EventMessage] ProcedureValidationResult result,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionProcedureValidationChangedCalled(logger,
            result.AgentSerializationViolations.Count,
            result.HasViolations);
        return result;
    }

    /// <summary>
    ///     Creates an observable subscription for procedure validation result changes using
    ///     <see cref="IProcedureValidationTracker" />.  The tracker's <c>ValidationResults</c>
    ///     stream is already throttled and de-duplicated; this method simply connects to it.
    /// </summary>
    /// <param name="tracker">
    ///     The singleton validation tracker that combines all validator outputs into a single
    ///     reactive stream.
    /// </param>
    /// <param name="logger">Logger for subscription diagnostics.</param>
    /// <returns>
    ///     An observable sequence that emits a <see cref="ProcedureValidationResult" /> each
    ///     time the validation outcome changes.
    /// </returns>
    public static IObservable<ProcedureValidationResult> SubscribeToProcedureValidationChanged(
        [Service] IProcedureValidationTracker tracker,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribeToProcedureValidationChangedCalled(logger);
        return tracker.ValidationResults
            .Do(r => LogGraphqlSubscriptionProcedureValidationEmitted(logger,
                r.AgentSerializationViolations.Count));
    }

    /// <summary>
    ///     Subscription endpoint that notifies clients when the loaded procedure identity changes.
    ///     Emits <see cref="LoadedProcedureIdentityDto" /> when a procedure is loaded,
    ///     or <c>null</c> when no procedure is loaded.
    /// </summary>
    /// <param name="identity">The identity of the loaded procedure, or null when unloaded.</param>
    /// <returns>The loaded procedure identity to be sent to the subscribing client.</returns>
    [Subscribe(With = nameof(SubscribeToLoadedProcedureIdentityChanged))]
    public LoadedProcedureIdentityDto? LoadedProcedureIdentityChanged(
        [EventMessage] LoadedProcedureIdentityDto identity,
        [Service] ILogger<Subscription> logger)
    {
        var result = identity.IsLoaded ? identity : null;
        LogGraphqlSubscriptionLoadedProcedureIdentityChanged(logger,
            result?.Id.ToString() ?? "null", result?.Name ?? "null");
        return result;
    }

    /// <summary>
    ///     Creates an observable subscription for loaded procedure identity changes using the
    ///     <see cref="IProcedureOrchestrator" />'s <see cref="IProcedureOrchestrator.ProcedureChanges" /> observable.
    ///     Emits the procedure ID and name when loaded, or null when unloaded.
    /// </summary>
    /// <param name="orchestrator">The procedure orchestrator providing the reactive stream.</param>
    /// <param name="logger">Logger for subscription diagnostics.</param>
    /// <returns>An observable sequence that emits the loaded procedure identity or null when unloaded.</returns>
    public static IObservable<LoadedProcedureIdentityDto> SubscribeToLoadedProcedureIdentityChanged(
        [Service] IProcedureOrchestrator orchestrator,
        [Service] ILogger<Subscription> logger)
    {
        LogGraphqlSubscriptionSubscribeToLoadedProcedureIdentityChanged(logger);
        return orchestrator.ProcedureChanges
            .Select(id => id.HasValue
                ? new LoadedProcedureIdentityDto
                {
                    Id = id.Value,
                    Name = orchestrator.GetLoadedProcedureName() ?? ""
                }
                : LoadedProcedureIdentityDto.Unloaded)
            .Do(identity => LogGraphqlSubscriptionLoadedProcedureIdentityEmitted(logger,
                identity.IsLoaded ? identity.Id.ToString() : "unloaded"));
    }

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionTimingChanged called | CurrentTime={currentTime:F2}s, Progress={progress:F1}%, IsRunning={isRunning}")]
    static partial void LogGraphqlSubscriptionExecutiontimingchangedCalled(
        ILogger<Subscription> logger, double currentTime, double progress, bool isRunning);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToExecutionTimingChanged called - setting up execution timing subscription")]
    static partial void LogGraphqlSubscriptionSubscribetoexecutiontimingchangedCalled(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: NodesChanged called with {nodeCount} nodes")]
    static partial void LogGraphqlSubscriptionNodeschangedCalledWithNodecountNodes(ILogger<Subscription> logger,
        int nodeCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: SkillExecutionNode - ID: {nodeId}, ExecutionState: {executionState}, Progress: {progress}%, Start Time: {startTime}s, Duration: {duration}s, Finish Time: {finishTime}s, AgentId: {agentId}, SkillName: {skillName}")]
    static partial void LogGraphqlSubscriptionSkillexecutionnodeIdNodeidExecutionstateExecutionstateProgress(
        ILogger<Subscription> logger, Guid nodeId, string executionState, double progress, double startTime,
        double duration, double? finishTime, Guid agentId, string skillName);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: TaskNode - ID: {nodeId}, TaskName: {taskName}, StartTime: {startTime}s, Duration: {duration}s")]
    static partial void LogGraphqlSubscriptionTasknodeIdNodeidTasknameTasknameStarttimeStarttimeS(
        ILogger<Subscription> logger, Guid nodeId, string taskName, double startTime, double duration);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: TaskNode - ID: {nodeId}, TaskName: {taskName}, StartTime: {startTime}s, Duration: {duration}s, ParentId: {parentId}, Position: ({posX}, {posY})")]
    static partial void LogGraphqlSubscriptionTasknodeWithParent(
        ILogger<Subscription> logger, Guid nodeId, string taskName, double startTime, double duration, Guid? parentId,
        double posX, double posY);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: TaskNode - ID: {nodeId}, TaskName: {taskName}, StartTime: {startTime}s, Duration: {duration}s, ParentId: {parentId}, Position: ({posX}, {posY}), Height: {height}, Width: {width}, Extent: {extent}, Hidden: {hidden}")]
    static partial void LogGraphqlSubscriptionTasknodeWithParentAndExtent(
        ILogger<Subscription> logger, Guid nodeId, string taskName, double startTime, double duration, Guid? parentId,
        double posX, double posY, double? height, double? width, string extent, bool? hidden);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: RouterNode - ID: {nodeId}, Name: {routerName}, StartTime: {startTime}s, Duration: {duration}s, ParentId: {parentId}, Position: ({posX}, {posY}), Height: {height}, Width: {width}, BranchCount: {branchCount}, SelectedBranchTargetNodeId: {selectedBranchTargetNodeId}, Hidden: {hidden}")]
    static partial void LogGraphqlSubscriptionRouternodeWithParent(
        ILogger<Subscription> logger, Guid nodeId, string routerName, double startTime, double duration, Guid? parentId,
        double posX, double posY, double? height, double? width, int branchCount, Guid? selectedBranchTargetNodeId,
        bool? hidden);

    [LoggerMessage(LogLevel.Trace, "GraphQL Subscription: Node - ID: {nodeId}, Type: {nodeType}, Hidden: {hidden}")]
    static partial void LogGraphqlSubscriptionNodeIdNodeidTypeNodetype(ILogger<Subscription> logger, Guid nodeId,
        string nodeType, bool? hidden);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToNodesChanged called - setting up nodes subscription")]
    static partial void LogGraphqlSubscriptionSubscribetonodeschangedCalledSettingUpNodesSubscription(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: NodesChanged observable emitted {nodeCount} nodes")]
    static partial void LogGraphqlSubscriptionNodeschangedObservableEmittedNodecountNodes(ILogger<Subscription> logger,
        int nodeCount);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: DependencyEdgesChanged called with {edgeCount} edges")]
    static partial void LogGraphqlSubscriptionDependencyedgeschangedCalledWithEdgecountEdges(
        ILogger<Subscription> logger, int edgeCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: DependencyEdge - ID: {edgeId}, Source: {sourceId} ({sourceHandle}) -> Target: {targetId} ({targetHandle})")]
    static partial void LogGraphqlSubscriptionDependencyedgeIdEdgeidSourceSourceidSourcehandleTarget(
        ILogger<Subscription> logger, Guid edgeId, Guid sourceId, string sourceHandle, Guid targetId,
        string targetHandle);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToDependencyEdgesChanged called - setting up dependency edges subscription")]
    static partial void LogGraphqlSubscriptionSubscribetodependencyedgeschangedCalledSettingUpDependencyEdges(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: DependencyEdgesChanged observable emitted {edgeCount} edges")]
    static partial void LogGraphqlSubscriptionDependencyedgeschangedObservableEmittedEdgecountEdges(
        ILogger<Subscription> logger, int edgeCount);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: AgentsChanged called with {agentCount} agents")]
    static partial void LogGraphqlSubscriptionAgentschangedCalledWithAgentcountAgents(ILogger<Subscription> logger,
        int agentCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: Agent - ID: {agentId}, Name: {agentName}, State: {state}, SkillCount: {skillCount}, LastSeen: {lastSeen}")]
    static partial void LogGraphqlSubscriptionAgentIdAgentidNameAgentnameStateStateSkillcount(
        ILogger<Subscription> logger, Guid agentId, string agentName, AgentState state, int skillCount,
        string lastSeen);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToAgentsChanged called - setting up agents subscription")]
    static partial void LogGraphqlSubscriptionSubscribetoagentschangedCalledSettingUpAgentsSubscription(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: AgentsChanged observable emitted {agentCount} agents")]
    static partial void LogGraphqlSubscriptionAgentschangedObservableEmittedAgentcountAgents(
        ILogger<Subscription> logger, int agentCount);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: SkillsChanged called with {skillCount} skills")]
    static partial void LogGraphqlSubscriptionSkillschangedCalledWithSkillcountSkills(ILogger<Subscription> logger,
        int skillCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: Skill - ID: {skillId}, Name: {skillName}, Description: {description}, PropertyCount: {propertyCount}")]
    static partial void LogGraphqlSubscriptionSkillIdSkillidNameSkillnameDescriptionDescription(
        ILogger<Subscription> logger, Guid skillId, string skillName, string description,
        int propertyCount);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToSkillsChanged called - setting up skills subscription")]
    static partial void LogGraphqlSubscriptionSubscribetoskillschangedCalledSettingUpSkillsSubscription(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug, "GraphQL Subscription: SkillsChanged observable emitted {skillCount} skills")]
    static partial void LogGraphqlSubscriptionSkillschangedObservableEmittedSkillcountSkills(
        ILogger<Subscription> logger, int skillCount);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: PositionTagsChanged called with {positionTagCount} position tags")]
    static partial void LogGraphqlSubscriptionPositiontagschangedCalledWithPositiontagcountPositionTags(
        ILogger<Subscription> logger, int positionTagCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: PositionTag - ID: {positionTagId}, Tag: {tag}, Position: ({x}, {y})")]
    static partial void LogGraphqlSubscriptionPositiontagIdPositiontagidTagTagPositionXy(ILogger<Subscription> logger,
        Guid positionTagId, string tag, double x, double y);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToPositionTagsChanged called - setting up position tags subscription")]
    static partial void LogGraphqlSubscriptionSubscribetopositiontagschangedCalledSettingUpPositionTagsSubscription(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: PositionTagsChanged observable emitted {positionTagCount} position tags")]
    static partial void LogGraphqlSubscriptionPositiontagschangedObservableEmittedPositiontagcountPositionTags(
        ILogger<Subscription> logger, int positionTagCount);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: SceneObjectsChanged called with {sceneObjectCount} scene objects")]
    static partial void LogGraphqlSubscriptionSceneobjectschangedCalledWithSceneobjectcountSceneObjects(
        ILogger<Subscription> logger, int sceneObjectCount);

    [LoggerMessage(LogLevel.Trace,
        "GraphQL Subscription: SceneObject - ID: {sceneObjectId}, Name: {name}, Position: ({x}, {y})")]
    static partial void LogGraphqlSubscriptionSceneobjectIdSceneobjectidNameNamePositionXy(ILogger<Subscription> logger,
        Guid sceneObjectId, string name, double x, double y);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToSceneObjectsChanged called - setting up scene objects subscription")]
    static partial void LogGraphqlSubscriptionSubscribetosceneobjectschangedCalledSettingUpSceneObjectsSubscription(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: SceneObjectsChanged observable emitted {sceneObjectCount} scene objects")]
    static partial void LogGraphqlSubscriptionSceneobjectschangedObservableEmittedSceneobjectcountSceneObjects(
        ILogger<Subscription> logger, int sceneObjectCount);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionEvents called with {eventType} event for skill {skillId}")]
    static partial void LogGraphqlSubscriptionExecutioneventsCalledWithEventtypeEventForSkillSkillid(
        ILogger<Subscription> logger, ExecutionEventType eventType, Guid skillId);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionEvent - Skill: {skillId}, Event: {eventType}, Timestamp: {timestamp}")]
    static partial void LogGraphqlSubscriptionExecutioneventSkillSkillidEventEventtypeTimestampTimestamp(
        ILogger<Subscription> logger, Guid skillId, ExecutionEventType eventType, DateTimeOffset timestamp);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToExecutionEvents called - setting up execution events subscription")]
    static partial void LogGraphqlSubscriptionSubscribetoexecutioneventsCalledSettingUpExecutionEvents(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionEvents observable emitted {eventType} event for skill {skillId}")]
    static partial void LogGraphqlSubscriptionExecutioneventsObservableEmittedEventtypeEventForSkillSkillid(
        ILogger<Subscription> logger, ExecutionEventType eventType, Guid skillId);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionEventsByType called with {eventType} event for skill {skillId}, Filter: {filter}")]
    static partial void LogGraphqlSubscriptionExecutioneventsbytypeCalledWithEventtypeEventForSkillSkillidFilterFilter(
        ILogger<Subscription> logger, ExecutionEventType eventType, Guid skillId, string filter);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToExecutionEventsByType called with filter: {filter}")]
    static partial void LogGraphqlSubscriptionSubscribetoexecutioneventsbytypeCalledWithFilterFilter(
        ILogger<Subscription> logger, string filter);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ExecutionEventsByType observable emitted {eventType} event for skill {skillId}")]
    static partial void LogGraphqlSubscriptionExecutioneventsbytypeObservableEmittedEventtypeEventForSkillSkillid(
        ILogger<Subscription> logger, ExecutionEventType eventType, Guid skillId);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ProcedureVariablesChanged called with {variableCount} variable(s)")]
    static partial void LogGraphqlSubscriptionProcedureVariablesChanged(
        ILogger<Subscription> logger, int variableCount);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToProcedureVariablesChanged called - setting up procedure variables subscription")]
    static partial void LogGraphqlSubscriptionSubscribeToProcedureVariablesChanged(ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ProcedureVariables observable emitted {variableCount} variable(s)")]
    static partial void LogGraphqlSubscriptionProcedureVariablesEmitted(
        ILogger<Subscription> logger, int variableCount);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: LoadedProcedureIdentityChanged called - Id: {procedureId}, Name: {procedureName}")]
    static partial void LogGraphqlSubscriptionLoadedProcedureIdentityChanged(
        ILogger<Subscription> logger, string procedureId, string procedureName);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToLoadedProcedureIdentityChanged called - setting up procedure identity subscription")]
    static partial void LogGraphqlSubscriptionSubscribeToLoadedProcedureIdentityChanged(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: LoadedProcedureIdentity observable emitted {procedureId}")]
    static partial void LogGraphqlSubscriptionLoadedProcedureIdentityEmitted(
        ILogger<Subscription> logger, string procedureId);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ProcedureValidationChanged called | AgentSerializationViolationCount={violationCount}, HasViolations={hasViolations}")]
    static partial void LogGraphqlSubscriptionProcedureValidationChangedCalled(
        ILogger<Subscription> logger, int violationCount, bool hasViolations);

    [LoggerMessage(LogLevel.Information,
        "GraphQL Subscription: SubscribeToProcedureValidationChanged called - setting up procedure validation subscription")]
    static partial void LogGraphqlSubscriptionSubscribeToProcedureValidationChangedCalled(
        ILogger<Subscription> logger);

    [LoggerMessage(LogLevel.Debug,
        "GraphQL Subscription: ProcedureValidation observable emitted | AgentSerializationViolationCount={violationCount}")]
    static partial void LogGraphqlSubscriptionProcedureValidationEmitted(
        ILogger<Subscription> logger, int violationCount);
}