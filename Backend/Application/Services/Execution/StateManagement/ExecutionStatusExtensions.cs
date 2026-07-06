namespace FHOOE.Freydis.Application.Services.Execution.StateManagement;

/// <summary>
///     Extension helpers for <see cref="ExecutionStatus" />.
/// </summary>
public static class ExecutionStatusExtensions
{
    /// <summary>
    ///     Indicates whether the given status is terminal — i.e. once entered, the
    ///     skill execution never transitions to another status.
    /// </summary>
    /// <param name="status">The status to inspect.</param>
    /// <returns>
    ///     <c>true</c> when <paramref name="status" /> is
    ///     <see cref="ExecutionStatus.Completed" />, <see cref="ExecutionStatus.Failed" />,
    ///     or <see cref="ExecutionStatus.NotSelected" />; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     The status-side counterpart of
    ///     <c>FHOOE.Freydis.Application.Services.Execution.Events.ExecutionEventTypeExtensions.IsTerminal</c>
    ///     (which classifies the bus events that produce these statuses). The terminal set
    ///     matches the Lean predicate <c>ExecutionStatus.isTerminal</c> in
    ///     <c>Sunstone/Sunstone/Common/ExecutionStatus.lean</c>. The
    ///     <c>SkillExecutionStateManager.UpdateState</c> guard uses this predicate to
    ///     reject mutations on a state that has already reached a terminal status,
    ///     providing defense-in-depth alignment between the C# field and the bus.
    /// </remarks>
    public static bool IsTerminal(this ExecutionStatus status)
    {
        return status is ExecutionStatus.Completed
            or ExecutionStatus.Failed
            or ExecutionStatus.NotSelected;
    }
}