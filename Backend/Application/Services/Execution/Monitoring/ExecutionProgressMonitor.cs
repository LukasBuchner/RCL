using FHOOE.Freydis.Application.Services.Execution.StateManagement;

namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Service responsible for monitoring and calculating execution progress.
///     Provides centralized logic for determining completion status and overall progress.
/// </summary>
public class ExecutionProgressMonitor : IExecutionProgressMonitor
{
    private readonly ISkillExecutionStateManager _stateManager;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionProgressMonitor" /> class.
    /// </summary>
    /// <param name="stateManager">The state manager for skill executions.</param>
    public ExecutionProgressMonitor(ISkillExecutionStateManager stateManager)
    {
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    }

    /// <inheritdoc />
    public double CalculateProgressPercentage()
    {
        var allStates = _stateManager.GetAllStates();
        var totalSkills = allStates.Count;

        if (totalSkills == 0) return 0.0;

        var totalProgress = 0.0;
        foreach (var state in allStates)
            totalProgress += state.ExecutionStatus switch
            {
                ExecutionStatus.Completed or ExecutionStatus.Failed or ExecutionStatus.NotSelected => 100.0,
                ExecutionStatus.Running => state.LastProgressPercentage ?? 0.0,
                _ => 0.0
            };

        return totalProgress / totalSkills;
    }

    /// <inheritdoc />
    public bool IsExecutionComplete()
    {
        var allStates = _stateManager.GetAllStates();
        var totalSkills = allStates.Count;

        if (totalSkills == 0) return true;

        var completedSkills = _stateManager.GetStatesByStatus(ExecutionStatus.Completed).Count;
        var failedSkills = _stateManager.GetStatesByStatus(ExecutionStatus.Failed).Count;
        var notSelectedSkills = _stateManager.GetStatesByStatus(ExecutionStatus.NotSelected).Count;

        return completedSkills + failedSkills + notSelectedSkills == totalSkills;
    }

    /// <inheritdoc />
    public bool IsExecutionSuccessful()
    {
        var failedSkills = _stateManager.GetStatesByStatus(ExecutionStatus.Failed).Count;
        return failedSkills == 0;
    }

    /// <inheritdoc />
    public Dictionary<string, int> GetExecutionStatistics()
    {
        var allStates = _stateManager.GetAllStates();
        var total = allStates.Count;
        var completed = _stateManager.GetStatesByStatus(ExecutionStatus.Completed).Count;
        var failed = _stateManager.GetStatesByStatus(ExecutionStatus.Failed).Count;
        var running = _stateManager.GetStatesByStatus(ExecutionStatus.Running).Count;
        var notSelected = _stateManager.GetStatesByStatus(ExecutionStatus.NotSelected).Count;
        var pending = total - completed - failed - running - notSelected;

        return new Dictionary<string, int>
        {
            ["Total"] = total,
            ["Pending"] = pending,
            ["Running"] = running,
            ["Completed"] = completed,
            ["Failed"] = failed,
            ["NotSelected"] = notSelected
        };
    }
}