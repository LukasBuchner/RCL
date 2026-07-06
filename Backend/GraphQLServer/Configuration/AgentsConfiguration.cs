namespace FHOOE.Freydis.GraphQLServer.Configuration;

/// <summary>
///     Configuration options for agent management.
///     Each agent type has its own sub-configuration with an independent <c>Enabled</c> flag,
///     allowing any combination of agent types to run simultaneously.
/// </summary>
public class AgentsConfiguration
{
    /// <summary>
    ///     Configuration for dummy agents.
    /// </summary>
    public DummyAgentsConfiguration DummyAgents { get; set; } = new();

    /// <summary>
    ///     Configuration for real agents.
    /// </summary>
    public RealAgentsConfiguration RealAgents { get; set; } = new();

    /// <summary>
    ///     Configuration for KUKA agents.
    /// </summary>
    public KukaAgentsConfiguration KukaAgents { get; set; } = new();

    /// <summary>
    ///     Configuration for Digital Twin agents connected via WebSocket.
    /// </summary>
    public DigitalTwinAgentsConfiguration DigitalTwin { get; set; } = new();
}

/// <summary>
///     Configuration for dummy agent operations.
/// </summary>
public class DummyAgentsConfiguration
{
    /// <summary>
    ///     Whether dummy agents are enabled. Defaults to <c>true</c> for development convenience.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Path to the JSON configuration file for dummy agents.
    /// </summary>
    public string? ConfigurationFile { get; set; }

    /// <summary>
    ///     Whether to automatically load dummy agents on startup.
    /// </summary>
    public bool AutoLoad { get; set; } = true;
}

/// <summary>
///     Configuration for real agent operations.
/// </summary>
public class RealAgentsConfiguration
{
    /// <summary>
    ///     Whether real agents are enabled. Defaults to <c>false</c> (not yet implemented).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Endpoint for discovering real agents.
    /// </summary>
    public string? DiscoveryEndpoint { get; set; }

    /// <summary>
    ///     Whether to automatically start real agent discovery on startup.
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    ///     Connection timeout in milliseconds for real agents.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30000;

    /// <summary>
    ///     Health check interval in milliseconds for real agents.
    /// </summary>
    public int HealthCheckInterval { get; set; } = 10000;
}

/// <summary>
///     Configuration for KUKA agent operations.
/// </summary>
public class KukaAgentsConfiguration
{
    /// <summary>
    ///     Whether KUKA agents are enabled. Defaults to <c>false</c> (requires hardware).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Path to the KUKA agents configuration file.
    /// </summary>
    public string? ConfigurationFile { get; set; }

    /// <summary>
    ///     Whether to automatically load KUKA agents at startup.
    /// </summary>
    public bool AutoLoad { get; set; } = true;
}

/// <summary>
///     Configuration for Digital Twin agent connections via WebSocket.
/// </summary>
public class DigitalTwinAgentsConfiguration
{
    /// <summary>
    ///     Whether Digital Twin agents are enabled. Defaults to <c>true</c> (always available for VR connections).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Default nominal execution duration in seconds, used when the Twin does not respond
    ///     to a duration estimation query within the timeout.
    /// </summary>
    public double NominalDurationSeconds { get; set; } = 5.0;

    /// <summary>
    ///     Interval in seconds between keepalive ping messages sent to connected Digital Twins.
    /// </summary>
    public double PingIntervalSeconds { get; set; } = 10.0;

    /// <summary>
    ///     Timeout in seconds to wait for a duration estimation response from the Twin.
    /// </summary>
    public double EstimateTimeoutSeconds { get; set; } = 2.0;

    /// <summary>
    ///     Maximum concurrent skill executions per Digital Twin agent.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 1;

    /// <summary>
    ///     WebSocket receive buffer size in bytes.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;
}