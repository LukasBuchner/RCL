using System.Reactive;
using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Properties;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using AgentProgress = FHOOE.Freydis.Agents.Agents.SkillExecutionProgress;
using ValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Coordination;

/// <summary>
///     Coordinates skill execution on agents and publishes execution events.
///     Publishes Start event when agent actually begins execution (on first progress update),
///     Finish event when skill completes. This ensures Start events reflect actual execution timing.
///     Supports property binding through variable context for input/output variable resolution.
/// </summary>
public sealed class SkillExecutionCoordinator(
    ISkillExecutionEventBus eventBus,
    IRuntimeAgentProvider agentProvider,
    IPropertyBindingService propertyBindingService,
    ISceneEntityResolver sceneEntityResolver,
    TimeProvider timeProvider,
    ILogger<SkillExecutionCoordinator> logger,
    ILogger<PipelineEvents> pipelineLogger)
    : ISkillExecutionCoordinator
{
    private readonly IRuntimeAgentProvider _agentProvider =
        agentProvider ?? throw new ArgumentNullException(nameof(agentProvider));

    private readonly ISkillExecutionEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

    private readonly ILogger<SkillExecutionCoordinator> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ILogger<PipelineEvents> _pipeline =
        pipelineLogger ?? throw new ArgumentNullException(nameof(pipelineLogger));

    private readonly IPropertyBindingService _propertyBindingService =
        propertyBindingService ?? throw new ArgumentNullException(nameof(propertyBindingService));

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>
    ///     Executes a skill on its assigned agent and publishes Start/Finish events.
    /// </summary>
    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid skillNodeId,
        Skill skill,
        Guid agentId,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken)
    {
        return CreateExecutionPipeline(
            skillNodeId, skill, agentId, variableContext,
            false,
            (agent, executionId, resolvedSkill) =>
                agent.ExecuteSkillAsync(executionId, resolvedSkill, cancellationToken));
    }

    /// <summary>
    ///     Executes an adaptive skill with dynamic schedule updates and event-driven completion.
    /// </summary>
    public IObservable<SkillExecutionProgress> ExecuteAdaptiveSkillAsync(
        Guid skillNodeId,
        Skill skill,
        Guid agentId,
        double initialDuration,
        IObservable<double> plannedFinishTimes,
        IObservable<Unit> finishSignal,
        VariableContextEntity? variableContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(finishSignal);

        return CreateExecutionPipeline(
            skillNodeId, skill, agentId, variableContext,
            true,
            (agent, executionId, resolvedSkill) =>
            {
                var resolvedSkillName = resolvedSkill.Name ?? "Unnamed";
                var loggedPlannedFinishTimes = plannedFinishTimes
                    .Do(pf =>
                    {
                        var pfStr = $"{pf:F1}";
                        _pipeline.LogAgentReceivedPlannedFinish(agent.Name, pfStr, resolvedSkillName, skillNodeId);
                    });
                return agent.ExecuteSkillAdaptivelyAsync(
                    executionId, resolvedSkill, initialDuration,
                    loggedPlannedFinishTimes, finishSignal, cancellationToken);
            },
            initialDuration);
    }

    /// <summary>
    ///     Shared execution pipeline for both regular and adaptive skill execution.
    ///     Uses .Select(FromAsync).Concat() instead of Subscribe(async) to preserve Rx.NET
    ///     sequential delivery, deterministic disposal, and error propagation guarantees.
    /// </summary>
    private IObservable<SkillExecutionProgress> CreateExecutionPipeline(
        Guid skillNodeId,
        Skill skill,
        Guid agentId,
        VariableContextEntity? variableContext,
        bool isAdaptive,
        Func<IRuntimeAgent, Guid, Skill, IObservable<AgentProgress>> invokeAgent,
        double? initialDuration = null)
    {
        ArgumentNullException.ThrowIfNull(skill);

        var skillType = isAdaptive ? "adaptive skill" : "skill";
        var agent = _agentProvider.GetRuntimeAgent(agentId);
        var agentName = agent?.Name ?? $"Unknown Agent ({agentId})";
        var skillName = skill.Name ?? "Unnamed Skill";

        if (isAdaptive)
        {
            var durationStr = $"{initialDuration:F1}";
            _pipeline.LogAdaptiveDispatchToAgent(skillName, skillNodeId, agentName, durationStr);
            var adaptiveCoordinatingInfo =
                $"Coordinating adaptive skill execution with initial duration {durationStr}s";
            _logger.LogExecutionEvent(
                "COORDINATING_START",
                skillNodeId,
                skillName,
                agentId: agentId,
                state: "PENDING",
                eventType: "TRIGGER",
                isAdaptive: true,
                additionalInfo: adaptiveCoordinatingInfo);
        }
        else
        {
            _pipeline.LogDispatchToAgent(skillName, skillNodeId, agentName);
            _logger.LogExecutionEvent(
                "COORDINATING_START",
                skillNodeId,
                skillName,
                agentId: agentId,
                state: "PENDING",
                eventType: "TRIGGER",
                additionalInfo: "Coordinating skill execution with agent");
        }

        return Observable.Create<SkillExecutionProgress>(async (observer, ct) =>
        {
            try
            {
                var runtimeAgent = _agentProvider.GetRuntimeAgent(agentId);
                if (runtimeAgent == null)
                {
                    _logger.LogAgentNotFound(agentName, agentId, skillType, skillName, skillNodeId);
                    observer.OnError(new InvalidOperationException($"Agent '{agentName}' ({agentId}) not found"));
                    return () => { };
                }

                var skillToExecute = await ResolveInputBindings(
                    skill, variableContext, skillType, skillName, observer);
                if (skillToExecute == null)
                    return () => { };

                // Refresh PositionTag/SceneObject properties from live cache so
                // execution always uses current data, not stale node snapshots.
                skillToExecute = sceneEntityResolver.RefreshSceneEntityProperties(skillToExecute);

                var startEventPublished = false;
                var executionId = Guid.NewGuid();

                var subscription = invokeAgent(runtimeAgent, executionId, skillToExecute)
                    .Select(agentProgress => Observable.FromAsync(async token =>
                    {
                        if (!startEventPublished)
                        {
                            _pipeline.LogAgentStarted(agentName, skillName, skillNodeId);
                            PublishEvent(skillNodeId, skillName, ExecutionEventType.Start);
                            startEventPublished = true;
                        }

                        var mapped = MapProgress(skillNodeId, agentProgress);

                        if (agentProgress is { CompletedSuccessfully: false, Error: null })
                            PublishProgressEvent(skillNodeId, skillName, mapped.Progress, agentProgress);

                        if (agentProgress.CompletedSuccessfully)
                        {
                            await ApplyOutputBindings(
                                skill, agentProgress, variableContext, skillType, skillName);
                            if (token.IsCancellationRequested) return mapped;

                            WriteOutputsToContext(
                                agentProgress, variableContext, skillType, skillName);

                            _pipeline.LogAgentCompleted(agentName, skillName, skillNodeId);
                            PublishEvent(skillNodeId, skillName, ExecutionEventType.Finish);
                        }

                        if (agentProgress.Error != null)
                        {
                            _logger.LogSkillExecutionFailed(
                                skillType, skillName, agentName, agentProgress.Error.Message);
                            PublishFailedEvent(skillNodeId, skillName, agentProgress.Error.Message);
                        }

                        return mapped;
                    }))
                    .Concat()
                    .Subscribe(
                        observer.OnNext,
                        error =>
                        {
                            _logger.LogSkillExecutionError(error, skillType, skillName, agentName);
                            observer.OnError(error);
                        },
                        observer.OnCompleted);

                return () => subscription.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogSkillStartFailed(ex, skillType, skillName, agentName);
                observer.OnError(ex);
                return () => { };
            }
        });
    }

    /// <summary>
    ///     Resolves input property bindings from the variable context before execution.
    ///     Returns null if binding resolution fails (error already signaled to observer).
    /// </summary>
    private async Task<Skill?> ResolveInputBindings(
        Skill skill,
        VariableContextEntity? variableContext,
        string skillType,
        string skillName,
        IObserver<SkillExecutionProgress> observer)
    {
        if (variableContext == null || !skill.Properties.Any(p => p.Binding != null))
            return skill;

        try
        {
            _logger.LogResolvingInputBindings(skillType, skillName);
            var resolvedInputs =
                await _propertyBindingService.ResolveInputBindingsAsync(skill, variableContext);

            if (resolvedInputs.Count > 0)
            {
                _logger.LogResolvedInputBindings(resolvedInputs.Count, skillType, skillName);
                return ApplyInputBindingsToSkill(skill, resolvedInputs);
            }

            return skill;
        }
        catch (VariableNotFoundException ex)
        {
            _logger.LogVariableNotFound(ex, ex.VariableName, skillType, skillName);
            observer.OnError(ex);
            return null;
        }
        catch (VariableTypeMismatchException ex)
        {
            _logger.LogTypeMismatch(ex, skillType, skillName, ex.Message);
            observer.OnError(ex);
            return null;
        }
    }

    /// <summary>
    ///     Applies output bindings from skill execution results back to the variable context.
    /// </summary>
    private async Task ApplyOutputBindings(
        Skill skill,
        AgentProgress agentProgress,
        VariableContextEntity? variableContext,
        string skillType,
        string skillName)
    {
        if (variableContext == null || agentProgress.Outputs == null ||
            !skill.Properties.Any(p => p.Binding != null))
            return;

        try
        {
            _logger.LogApplyingOutputBindings(skillType, skillName);
            await _propertyBindingService.ApplyOutputBindingsAsync(
                skill, agentProgress.Outputs, variableContext);
            _logger.LogAppliedOutputBindings(skillType, skillName);
        }
        catch (Exception ex)
        {
            _logger.LogOutputBindingsFailed(ex, skillType, skillName, ex.Message);
        }
    }

    /// <summary>
    ///     Writes skill output values directly into the variable context.
    /// </summary>
    private void WriteOutputsToContext(
        AgentProgress agentProgress,
        VariableContextEntity? variableContext,
        string skillType,
        string skillName)
    {
        if (variableContext == null || agentProgress.Outputs == null)
            return;

        try
        {
            foreach (var (outputName, outputValue) in agentProgress.Outputs)
                variableContext.SetValue(outputName, outputValue, $"Skill:{skillName}");
        }
        catch (Exception ex)
        {
            _logger.LogVariableWriteFailed(ex, skillType, skillName, ex.Message);
        }
    }

    /// <summary>
    ///     Maps agent-level progress to the coordinator-level progress model.
    /// </summary>
    private static SkillExecutionProgress MapProgress(Guid skillNodeId, AgentProgress agentProgress)
    {
        return new SkillExecutionProgress
        {
            SkillId = skillNodeId,
            Progress = Math.Clamp(
                agentProgress.CurrentTimeIntoExecution /
                Math.Max(agentProgress.EstimatedTotalDuration, 0.001),
                0.0, 1.0),
            StatusMessage = agentProgress.StatusMessage,
            IsCompleted = agentProgress.CompletedSuccessfully,
            IsFailed = agentProgress.Error != null,
            ErrorMessage = agentProgress.Error?.Message,
            Outputs = agentProgress.Outputs
        };
    }

    /// <summary>
    ///     Publishes an execution event to the event bus.
    /// </summary>
    private void PublishEvent(Guid skillId, string skillName, ExecutionEventType eventType)
    {
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = eventType,
            Timestamp = _timeProvider.GetUtcNow()
        };

        // Log before publishing: Subject.OnNext dispatches synchronously to all subscribers,
        // so steps 5+ run inline before PublishEvent returns.
        var eventTypeName = eventType.ToString();
        _pipeline.LogEventPublished(eventTypeName, skillName, skillId);

        _eventBus.PublishEvent(executionEvent);
    }

    /// <summary>
    ///     Publishes a progress event to the event bus with detailed progress data.
    /// </summary>
    private void PublishProgressEvent(Guid skillId, string skillName, double progressPercentage,
        AgentProgress agentProgress)
    {
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Progress,
            Timestamp = _timeProvider.GetUtcNow(),
            ProgressPercentage = progressPercentage,
            ProgressData = agentProgress
        };

        // Log before publishing: Subject.OnNext dispatches synchronously to all subscribers.
        _pipeline.LogEventPublished("Progress", skillName, skillId);

        _eventBus.PublishEvent(executionEvent);
    }

    /// <summary>
    ///     Publishes a failed event to the event bus with the error message from the agent.
    /// </summary>
    /// <param name="skillId">The ID of the skill that failed.</param>
    /// <param name="skillName">The display name of the skill (for logging).</param>
    /// <param name="errorMessage">The error message describing the failure reason.</param>
    private void PublishFailedEvent(Guid skillId, string skillName, string errorMessage)
    {
        var executionEvent = new ExecutionEvent
        {
            SkillId = skillId,
            EventType = ExecutionEventType.Failed,
            Timestamp = _timeProvider.GetUtcNow(),
            ErrorMessage = errorMessage
        };

        _pipeline.LogEventPublished("Failed", skillName, skillId);

        _eventBus.PublishEvent(executionEvent);
    }

    /// <summary>
    ///     Creates a copy of the skill with input property values updated from resolved bindings.
    /// </summary>
    private static Skill ApplyInputBindingsToSkill(Skill originalSkill, Dictionary<string, object> resolvedInputs)
    {
        var updatedProperties = originalSkill.Properties.Select(prop =>
        {
            if (resolvedInputs.TryGetValue(prop.Name, out var resolvedValue))
                return prop with { Value = CreateTypedValue(prop.Value.Type, resolvedValue) };
            return prop;
        }).ToList();

        return originalSkill with { Properties = updatedProperties };
    }

    /// <summary>
    ///     Creates a TypedValue from a resolved binding value.
    /// </summary>
    private static TypedValue CreateTypedValue(ValueType type, object value)
    {
        return new TypedValue
        {
            Type = type,
            Value = value
        };
    }
}