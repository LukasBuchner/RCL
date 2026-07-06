namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Represents the execution status of a skill.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    ///     Skill has not started execution yet.
    /// </summary>
    NotStarted,

    /// <summary>
    ///     Skill is scheduled for execution.
    /// </summary>
    Scheduled,

    /// <summary>
    ///     Skill is currently running.
    /// </summary>
    Running,

    /// <summary>
    ///     Skill has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    ///     Skill execution failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     Skill was not selected for execution because it resides in a non-selected router branch.
    /// </summary>
    NotSelected
}