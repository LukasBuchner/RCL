using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Triggering;

/// <summary>
///     Handles triggering and lifecycle management of skill executions.
///     Manages both regular and adaptive skills, including BehaviorSubject-based
///     planned finish time updates and re-entry prevention.
/// </summary>
public sealed class SkillTriggerHandler(
    ISkillExecutionEventBus eventBus,
    ISkillExecutionCoordinator coordinator,
    IRuntimeAgentProvider agentProvider,
    ILogger<SkillTriggerHandler> logger,
    ILogger<PipelineEvents> pipelineLogger)
    : ISkillTriggerHandler
{
    private readonly IRuntimeAgentProvider _agentProvider =
        agentProvider ?? throw new ArgumentNullException(nameof(agentProvider));

    private readonly ISkillExecutionCoordinator _coordinator =
        coordinator ?? throw new ArgumentNullException(nameof(coordinator));

    private readonly ISkillExecutionEventBus _eventBus =
        eventBus ?? throw new ArgumentNullException(nameof(eventBus));

    private readonly ILogger<SkillTriggerHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ILogger<PipelineEvents> _pipeline =
        pipelineLogger ?? throw new ArgumentNullException(nameof(pipelineLogger));

    private readonly ConcurrentDictionary<Guid, BehaviorSubject<double>>
        _plannedFinishSubjects = new();

    private readonly ConcurrentDictionary<Guid, byte>
        _triggeringSkills = new();

    /// <inheritdoc />
    public IDisposable? TriggerSkill(
        Guid skillId,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        DependencyGraph dependencyGraph,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken)
    {
        var resolvedSkillName = ResolveSkillName(skillId, skillNodes);
        if (!_triggeringSkills.TryAdd(skillId, 0))
        {
            _logger.LogSkillAlreadyTriggering(resolvedSkillName, skillId);
            return null;
        }

        if (!skillNodes.TryGetValue(skillId, out var skillNode))
        {
            _logger.LogSkillNodeNotFound(resolvedSkillName, skillId);
            _triggeringSkills.TryRemove(skillId, out _);
            return null;
        }

        var prerequisites = dependencyGraph.GetPrerequisites(skillId);
        if (prerequisites == null)
        {
            _logger.LogPrerequisitesNotFound(resolvedSkillName, skillId);
            _triggeringSkills.TryRemove(skillId, out _);
            return null;
        }

        var skill = skillNode.SkillExecutionTask.Skill;
        var agentId = skillNode.SkillExecutionTask.AgentId;

        // Get agent name for user-friendly logging
        var agent = _agentProvider.GetRuntimeAgent(agentId);
        var agentName = agent?.Name ?? $"Unknown Agent ({agentId})";
        var skillName = skill.Name;

        if (prerequisites.IsAdaptive)
        {
            _pipeline.LogAdaptivePrerequisitesMet(skillName, skillId, agentName);

            var adaptiveInfo =
                $"Agent '{agentName}', domain skill '{skill.Id}', {prerequisites.FinishPrerequisites.Count} finish prerequisites";
            _logger.LogExecutionEvent(
                "TRIGGERING_SKILL",
                skillId,
                skillName,
                agentId: agentId,
                state: "PENDING",
                eventType: "TRIGGER",
                isAdaptive: true,
                additionalInfo: adaptiveInfo);

            // Get initial duration from the task (this will be the initial planned finish time)
            var initialDuration = skillNode.SkillExecutionTask.Duration;

            // Create BehaviorSubject for planned finish time updates (starts with initial duration)
            var plannedFinishSubject = new BehaviorSubject<double>(initialDuration);
            _plannedFinishSubjects[skillId] = plannedFinishSubject;

            // Create finish signal from finish prerequisites (convert ExecutionEvent to Unit)
            var finishSignal = CreateFinishSignal(prerequisites.FinishPrerequisites)
                .Select(_ => Unit.Default);

            // Execute adaptively - pass skillId (SkillExecutionNode ID) for event tracking
            return _coordinator.ExecuteAdaptiveSkillAsync(
                    skillId,
                    skill,
                    agentId,
                    initialDuration,
                    plannedFinishSubject.AsObservable(),
                    finishSignal,
                    variableContext,
                    cancellationToken)
                .Subscribe(
                    _ => { },
                    error =>
                    {
                        _logger.LogTriggerExecutionError(error, "adaptive skill", skillName, skillId);
                        _eventBus.PublishEvent(new ExecutionEvent
                        {
                            SkillId = skillId,
                            EventType = ExecutionEventType.Failed,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    },
                    () =>
                    {
                        _logger.LogTriggerStreamCompleted("adaptive skill", skillName, skillId);

                        // Remove and dispose the BehaviorSubject so subsequent reschedules
                        // don't push planned finish times to a completed skill.
                        if (_plannedFinishSubjects.TryRemove(skillId, out var subject))
                            subject.Dispose();
                    });
        }
        else
        {
            _pipeline.LogPrerequisitesMet(skillName, skillId, agentName);

            var regularInfo = $"Agent '{agentName}', domain skill '{skill.Id}'";
            _logger.LogExecutionEvent(
                "TRIGGERING_SKILL",
                skillId,
                skillName,
                agentId: agentId,
                state: "PENDING",
                eventType: "TRIGGER",
                isAdaptive: false,
                additionalInfo: regularInfo);

            // Execute normally - pass skillId (SkillExecutionNode ID) for event tracking
            return _coordinator.ExecuteSkillAsync(
                    skillId,
                    skill,
                    agentId,
                    variableContext,
                    cancellationToken)
                .Subscribe(
                    _ => { },
                    error =>
                    {
                        _logger.LogTriggerExecutionError(error, "skill", skillName, skillId);
                        _eventBus.PublishEvent(new ExecutionEvent
                        {
                            SkillId = skillId,
                            EventType = ExecutionEventType.Failed,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    },
                    () => _logger.LogTriggerStreamCompleted("skill", skillName, skillId));
        }
    }

    /// <inheritdoc />
    public void UpdatePlannedFinish(Guid skillId, double newPlannedFinishTime)
    {
        if (_plannedFinishSubjects.TryGetValue(skillId, out var subject))
        {
            var plannedFinishStr = $"{newPlannedFinishTime:F1}";
            var skillIdStr = skillId.ToString();
            _pipeline.LogForwardingPlannedFinish(plannedFinishStr, skillIdStr, skillId);
            subject.OnNext(newPlannedFinishTime);
        }
        else
        {
            _logger.LogPlannedFinishUpdateSkipped(skillId);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        foreach (var subject in _plannedFinishSubjects.Values) subject.Dispose();
        _plannedFinishSubjects.Clear();
        _triggeringSkills.Clear();
    }

    private static string ResolveSkillName(Guid skillId, IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes)
    {
        return skillNodes.TryGetValue(skillId, out var node)
            ? node.SkillExecutionTask.Skill.Name
            : "Unknown Skill";
    }

    /// <summary>
    ///     Creates an observable that emits the last ExecutionEvent when all finish prerequisites are met.
    /// </summary>
    private IObservable<ExecutionEvent> CreateFinishSignal(IReadOnlyList<EventPrerequisite> finishPrerequisites)
    {
        if (finishPrerequisites.Count == 0)
            return Observable.Never<ExecutionEvent>();

        var prerequisiteObservables = finishPrerequisites
            .Select(CreateSinglePrerequisiteObservable)
            .ToList();

        return prerequisiteObservables.CombineLatest()
            .Select(events => events.Last())
            .Take(1);
    }

    private IObservable<ExecutionEvent> CreateSinglePrerequisiteObservable(EventPrerequisite prerequisite)
    {
        var requiredEventType = prerequisite.RequiredEventType == EventTriggerType.Start
            ? ExecutionEventType.Start
            : ExecutionEventType.Finish;

        return _eventBus.AllEvents
            .Where(e => e.SkillId == prerequisite.DependencySkillId &&
                        (e.EventType == requiredEventType ||
                         e.EventType == ExecutionEventType.Failed ||
                         e.EventType == ExecutionEventType.NotSelected))
            .Take(1);
    }
}