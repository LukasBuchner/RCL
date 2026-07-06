namespace FHOOE.Freydis.Scheduling.Core;

/// <summary>
///     Thrown when the schedule model is malformed (nulls, duplicates, etc.).
/// </summary>
[Serializable]
public sealed class ScheduleModelException : Exception
{
    public ScheduleModelException(string message)
        : base(message)
    {
    }

    public ScheduleModelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Creates a new ScheduleModelException specifying which parameter was invalid.
    /// </summary>
    /// <param name="paramName">Name of the offending parameter or property.</param>
    /// <param name="message">Explanation of the error.</param>
    public ScheduleModelException(string paramName, string message)
        : base($"{message} (Parameter '{paramName}')")
    {
        ParamName = paramName;
    }

    /// <summary>
    ///     The name of the parameter or property that caused the error.
    /// </summary>
    public string? ParamName { get; }
}

/// <summary>
///     Thrown when no schedule exists that satisfies every constraint.
/// </summary>
[Serializable]
public sealed class ScheduleInfeasibleException : Exception
{
    public ScheduleInfeasibleException()
    {
    }

    public ScheduleInfeasibleException(string message)
        : base(message)
    {
    }

    public ScheduleInfeasibleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Creates a new ScheduleInfeasibleException specifying which parameter was involved.
    /// </summary>
    /// <param name="paramName">Name of the offending parameter or property.</param>
    /// <param name="message">Explanation of why the schedule is infeasible.</param>
    public ScheduleInfeasibleException(string paramName, string message)
        : base($"{message} (Parameter '{paramName}')")
    {
        ParamName = paramName;
    }

    /// <summary>
    ///     The name of the parameter or property that caused the error (if applicable).
    /// </summary>
    public string? ParamName { get; }
}