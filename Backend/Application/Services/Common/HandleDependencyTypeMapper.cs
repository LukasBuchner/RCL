using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Common;

/// <summary>
///     Pure mapper translating dependency-edge handles into event and dependency types.
///     Shared by the execution dependency analyzer and the scheduling graph builder so both
///     layers derive the same <see cref="EventTriggerType" /> and <see cref="DependencyType" />
///     from the same handle strings.
/// </summary>
public static class HandleDependencyTypeMapper
{
    /// <summary>
    ///     Maps a single edge handle to the event type it requires from the dependency.
    ///     The handle is trimmed and compared case-insensitively: <c>"left"</c> yields
    ///     <see cref="EventTriggerType.Start" /> and <c>"right"</c> yields
    ///     <see cref="EventTriggerType.Finish" />. Any other value, including <see langword="null" />,
    ///     empty, whitespace, or an unrecognized token, yields <see cref="EventTriggerType.Finish" />.
    ///     The mapping is total and never throws.
    /// </summary>
    /// <param name="handle">The source or target handle from a dependency edge; may be <see langword="null" />.</param>
    /// <returns>
    ///     <see cref="EventTriggerType.Start" /> for <c>"left"</c>, otherwise
    ///     <see cref="EventTriggerType.Finish" />.
    /// </returns>
    public static EventTriggerType ToEventType(string? handle)
    {
        return handle?.Trim().ToLowerInvariant() switch
        {
            "left" => EventTriggerType.Start,
            "right" => EventTriggerType.Finish,
            _ => EventTriggerType.Finish
        };
    }

    /// <summary>
    ///     Maps a (sourceHandle, targetHandle) pair to the <see cref="DependencyType" /> it denotes.
    ///     Each handle is coerced to a <see cref="EventTriggerType" /> via <see cref="ToEventType" />
    ///     (so unknown or <see langword="null" /> handles become <see cref="EventTriggerType.Finish" />),
    ///     then the pair is mapped: (Finish, Start) to <see cref="DependencyType.FinishToStart" />,
    ///     (Start, Start) to <see cref="DependencyType.StartToStart" />,
    ///     (Start, Finish) to <see cref="DependencyType.StartToFinish" />, and
    ///     (Finish, Finish) to <see cref="DependencyType.FinishToFinish" />.
    ///     The mapping is total and never throws.
    /// </summary>
    /// <param name="sourceHandle">The handle on the source side of the edge; may be <see langword="null" />.</param>
    /// <param name="targetHandle">The handle on the target side of the edge; may be <see langword="null" />.</param>
    /// <returns>The <see cref="DependencyType" /> corresponding to the coerced handle pair.</returns>
    public static DependencyType ToDependencyType(string? sourceHandle, string? targetHandle)
    {
        var source = ToEventType(sourceHandle);
        var target = ToEventType(targetHandle);

        return (source, target) switch
        {
            (EventTriggerType.Finish, EventTriggerType.Start) => DependencyType.FinishToStart,
            (EventTriggerType.Start, EventTriggerType.Start) => DependencyType.StartToStart,
            (EventTriggerType.Start, EventTriggerType.Finish) => DependencyType.StartToFinish,
            _ => DependencyType.FinishToFinish
        };
    }
}