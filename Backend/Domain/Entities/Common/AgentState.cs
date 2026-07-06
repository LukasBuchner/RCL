namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Represents the operational state of an agent in the system.
/// </summary>
public enum AgentState
{
    /// <summary>
    ///     Agent is registered but not yet active.
    /// </summary>
    Registered,

    /// <summary>
    ///     Agent is currently active and operational.
    /// </summary>
    Active,

    /// <summary>
    ///     Agent is registered but currently inactive.
    /// </summary>
    Inactive,

    /// <summary>
    ///     Agent connection has been lost.
    /// </summary>
    Lost,

    /// <summary>
    ///     Agent has been decommissioned and is no longer in service.
    /// </summary>
    Decommissioned
}