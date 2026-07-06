namespace FHOOE.Freydis.Agents.Agents.DigitalTwin.Configuration;

/// <summary>
///     Configuration options for the Digital Twin agent connection and behaviour.
///     Bound from the "Agents:DigitalTwin" section of appsettings.json.
/// </summary>
public class DigitalTwinAgentConfiguration
{
    /// <summary>
    ///     Default nominal execution duration in seconds, used as a fallback when the Twin
    ///     does not respond to a duration estimation query within the timeout.
    /// </summary>
    public double NominalDurationSeconds { get; set; } = 5.0;

    /// <summary>
    ///     Interval in seconds between keepalive ping messages sent to connected Digital Twins.
    /// </summary>
    public double PingIntervalSeconds { get; set; } = 10.0;

    /// <summary>
    ///     Maximum time in seconds since the last received message (pong, progress, health, etc.)
    ///     before the connection is declared stale and forcibly closed.
    ///     Default of 30 seconds tolerates approximately two missed pongs at the default 10-second ping interval.
    /// </summary>
    public double PongTimeoutSeconds { get; set; } = 30.0;

    /// <summary>
    ///     Timeout in seconds to wait for a duration estimation response from the Twin
    ///     before falling back to <see cref="NominalDurationSeconds" />.
    /// </summary>
    public double EstimateTimeoutSeconds { get; set; } = 2.0;

    /// <summary>
    ///     Maximum number of concurrent skill executions a single Digital Twin agent can handle.
    ///     The agent reports itself as unavailable when this limit is reached.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 1;

    /// <summary>
    ///     Size of the WebSocket receive buffer in bytes.
    ///     Messages larger than this will be received in multiple chunks.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    ///     Minimum adaptive duration in seconds for the Hold Position skill.
    ///     The orchestrator will not schedule a hold shorter than this.
    /// </summary>
    public double HoldMinDurationSeconds { get; set; } = 1.0;
}