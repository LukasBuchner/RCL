namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Extension helpers for <see cref="ExecutionEventType" />.
/// </summary>
public static class ExecutionEventTypeExtensions
{
    /// <summary>
    ///     Indicates whether the given event type marks a skill as terminal — i.e.
    ///     once an event of this type fires, the skill never transitions to a non-terminal
    ///     state.
    /// </summary>
    /// <param name="eventType">The event type to inspect.</param>
    /// <returns>
    ///     <c>true</c> when <paramref name="eventType" /> is
    ///     <see cref="ExecutionEventType.Finish" />, <see cref="ExecutionEventType.Failed" />,
    ///     or <see cref="ExecutionEventType.NotSelected" />; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     The bus-side counterpart of
    ///     <c>FHOOE.Freydis.Application.Services.Execution.StateManagement.ExecutionStatusExtensions.IsTerminal</c>.
    ///     Each terminal event type maps one-to-one to a terminal
    ///     <c>ExecutionStatus</c>: <c>Finish → Completed</c>, <c>Failed → Failed</c>,
    ///     <c>NotSelected → NotSelected</c>. Mirrored on the Lean side as
    ///     <c>EventType.isTerminal</c> in <c>Sunstone/Sunstone/Common/EventType.lean</c>;
    ///     the <c>statusOf</c> projection in <c>ExecutionStatus.lean</c> uses the same
    ///     terminal set when reconstructing a node's status from the bus.
    /// </remarks>
    public static bool IsTerminal(this ExecutionEventType eventType)
    {
        return eventType is ExecutionEventType.Finish
            or ExecutionEventType.Failed
            or ExecutionEventType.NotSelected;
    }
}