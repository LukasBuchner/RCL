using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Dispatches execution events from the event bus, performing state transitions
///     and requesting reschedules. Handles Start, Finish, Failed, NotSelected, and Progress events.
/// </summary>
public sealed class ExecutionEventDispatcher(
    ISkillExecutionStateManager stateManager,
    IExecutionStateTransitionService stateTransitionService,
    ILogger<ExecutionEventDispatcher> logger,
    ILogger<PipelineEvents> pipelineLogger)
    : IExecutionEventDispatcher
{
    private readonly ILogger<ExecutionEventDispatcher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ILogger<PipelineEvents> _pipeline =
        pipelineLogger ?? throw new ArgumentNullException(nameof(pipelineLogger));

    private readonly ISkillExecutionStateManager _stateManager =
        stateManager ?? throw new ArgumentNullException(nameof(stateManager));

    private readonly IExecutionStateTransitionService _stateTransitionService =
        stateTransitionService ?? throw new ArgumentNullException(nameof(stateTransitionService));

    /// <inheritdoc />
    public void HandleExecutionEvent(
        ExecutionEvent executionEvent,
        IReadOnlyList<Node> currentNodes,
        DateTimeOffset executionStartTime,
        IObserver<RescheduleReason>? rescheduleRequests)
    {
        var timestamp = (executionEvent.Timestamp - executionStartTime).TotalSeconds;

        // Check if this is a RouterNode event (routers are in the dependency graph but don't have skill-like state)
        var routerNode = currentNodes.OfType<RouterNode>()
            .FirstOrDefault(n => n.Id == executionEvent.SkillId);

        if (routerNode != null)
        {
            // RouterNode events only trigger rescheduling, no state transitions needed
            var routerEventTypeName = executionEvent.EventType.ToString();
            _pipeline.LogRouterEventReceived(
                routerNode.RouterTask.Name, routerNode.Id, routerEventTypeName, timestamp);

            // Request re-scheduling after router evaluation (throttled via Rx.NET)
            rescheduleRequests?.OnNext(RescheduleReason.RouterEvaluated);
            return;
        }

        // Leafless container tasks fire Start/Finish to carry a dependency chain through an empty task or
        // branch, but have no skill-like state and are not tracked for completion, so their events require
        // no state transition or reschedule.
        var taskNode = currentNodes.OfType<TaskNode>()
            .FirstOrDefault(n => n.Id == executionEvent.SkillId);

        if (taskNode != null)
        {
            var leaflessEventTypeName = executionEvent.EventType.ToString();
            _pipeline.LogLeaflessContainerEventReceived(
                taskNode.Task.Name, taskNode.Id, leaflessEventTypeName, timestamp);
            return;
        }

        // Find the skill node by node ID
        var skillNode = currentNodes.OfType<SkillExecutionNode>()
            .FirstOrDefault(n => n.Id == executionEvent.SkillId);

        if (skillNode == null)
        {
            _pipeline.LogNodeNotFoundForEvent(executionEvent.SkillId);
            return;
        }

        var skillName = skillNode.SkillExecutionTask.Skill.Name;
        var agent = _stateManager.GetAssignedAgent(skillNode.Id);

        switch (executionEvent)
        {
            case { EventType: ExecutionEventType.Start }:
                {
                    _pipeline.LogOrchestratorEventReceived("Start", skillName, skillNode.Id);
                    if (agent != null)
                    {
                        _stateTransitionService.TransitionToRunning(skillNode.Id, agent, executionEvent.Timestamp);
                        _pipeline.LogStateUpdated("Running", skillName, skillNode.Id);
                    }
                    else
                    {
                        _pipeline.LogNoAgentForSkillStart(skillName, skillNode.Id);
                    }

                    rescheduleRequests?.OnNext(RescheduleReason.SkillStarted);
                    break;
                }
            case { EventType: ExecutionEventType.Finish }:
                _pipeline.LogOrchestratorEventReceived("Finish", skillName, skillNode.Id);
                _stateTransitionService.TransitionToCompleted(skillNode.Id, executionEvent.Timestamp);
                _pipeline.LogStateUpdated("Completed", skillName, skillNode.Id);

                rescheduleRequests?.OnNext(RescheduleReason.SkillFinished);
                break;
            case { EventType: ExecutionEventType.Failed }:
                _pipeline.LogOrchestratorEventReceived("Failed", skillName, skillNode.Id);
                if (executionEvent.ErrorMessage is null)
                    _pipeline.LogFailedEventMissingErrorMessage(skillName, skillNode.Id, "Unknown error");
                _stateTransitionService.TransitionToFailed(
                    skillNode.Id,
                    executionEvent.ErrorMessage ?? "Unknown error",
                    executionEvent.Timestamp);
                _pipeline.LogStateUpdated("Failed", skillName, skillNode.Id);

                rescheduleRequests?.OnNext(RescheduleReason.SkillFailed);
                break;
            case { EventType: ExecutionEventType.NotSelected }:
                _pipeline.LogOrchestratorEventReceived("NotSelected", skillName, skillNode.Id);
                _stateTransitionService.TransitionToNotSelected(skillNode.Id, executionEvent.Timestamp);
                _pipeline.LogStateUpdated("NotSelected", skillName, skillNode.Id);

                rescheduleRequests?.OnNext(RescheduleReason.SkillNotSelected);
                break;
            case { EventType: ExecutionEventType.Progress }:
                {
                    _pipeline.LogOrchestratorProgressReceived(skillName, skillNode.Id);

                    // Update progress in state manager
                    if (executionEvent is { ProgressPercentage: not null, ProgressData: not null })
                    {
                        var progressPercentage = executionEvent.ProgressPercentage.Value * 100.0;
                        _stateTransitionService.UpdateProgress(skillNode.Id, progressPercentage,
                            executionEvent.ProgressData);

                        var progressStr = $"{progressPercentage:F0}";
                        _pipeline.LogProgressUpdated(skillName, skillNode.Id, progressStr);

                        // Log structured progress event at Trace level (fires very frequently)
                        _logger.LogProgressEvent(
                            "REPORTING_PROGRESS",
                            skillNode.Id,
                            skillNode.SkillExecutionTask.Skill.Name,
                            agent?.Id,
                            progressPercentage,
                            timestamp);

                        rescheduleRequests?.OnNext(RescheduleReason.ProgressUpdate);
                    }

                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(executionEvent));
        }
    }
}