namespace FHOOE.Freydis.Agents.Agents.Kuka.Configuration;

/// <summary>
///     Root configuration for KUKA iiwa 14 agents loaded from JSON.
/// </summary>
public class KukaIiwa14AgentConfiguration
{
    /// <summary>
    ///     List of KUKA agent configurations.
    /// </summary>
    public List<KukaIiwa14AgentConfig> Agents { get; set; } = [];
}

/// <summary>
///     Configuration for a single KUKA iiwa 14 agent instance.
/// </summary>
public class KukaIiwa14AgentConfig
{
    /// <summary>
    ///     Unique identifier for the agent. If null, a new GUID will be generated.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    ///     Name of the agent (required).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     OPC UA endpoint URL (required).
    ///     Example: "opc.tcp://localhost:4840/kuka/iiwa14"
    /// </summary>
    public required string OpcUaEndpoint { get; set; }

    /// <summary>
    ///     Optional description of the agent.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Maximum number of concurrent skill executions.
    ///     Default: 1 (KUKA robots typically execute one skill at a time)
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 1;

    /// <summary>
    ///     Connection timeout in milliseconds.
    ///     Default: 5000ms (5 seconds)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 5000;

    /// <summary>
    ///     OPC UA security configuration.
    ///     If not specified, defaults to anonymous/insecure connection (suitable for development).
    /// </summary>
    public OpcUaSecurityConfig? Security { get; set; }

    /// <summary>
    ///     List of skills this agent can perform.
    /// </summary>
    public List<KukaSkillConfig> Skills { get; set; } = [];
}

/// <summary>
///     OPC UA security configuration for KUKA agent connections.
/// </summary>
public class OpcUaSecurityConfig
{
    /// <summary>
    ///     Whether to use anonymous authentication (no username/password).
    ///     Default: true (suitable for development/testing)
    /// </summary>
    public bool UseAnonymousAuthentication { get; set; } = true;

    /// <summary>
    ///     Whether to use secure channel encryption.
    ///     Default: false (no encryption, suitable for development/testing on localhost)
    ///     IMPORTANT: Should be true in production!
    /// </summary>
    public bool UseEncryption { get; set; }

    /// <summary>
    ///     Whether to automatically accept untrusted certificates.
    ///     Only relevant if UseEncryption is true.
    ///     Default: true (suitable for development/testing)
    ///     WARNING: Set to false in production environments!
    /// </summary>
    public bool AutoAcceptUntrustedCertificates { get; set; } = true;

    /// <summary>
    ///     Username for authenticated connections (if UseAnonymousAuthentication is false).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Password for authenticated connections (if UseAnonymousAuthentication is false).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    ///     Path to client certificate file (optional, for certificate-based authentication).
    /// </summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>
    ///     Whether to validate server certificates.
    ///     Only relevant if UseEncryption is true.
    ///     Default: false (suitable for development/testing)
    ///     IMPORTANT: Should be true in production!
    /// </summary>
    public bool ValidateServerCertificate { get; set; }

    /// <summary>
    ///     Path to the certificate store directory for OPC UA certificates.
    ///     If not specified, uses default: %AppData%/FHOOE.Freydis/OpcUa/PKI
    /// </summary>
    public string? CertificateStorePath { get; set; }
}

/// <summary>
///     KUKA-specific skill performance configuration.
/// </summary>
public class KukaSkillConfig
{
    /// <summary>
    ///     Reference to a shared skill definition ID.
    /// </summary>
    public Guid? SkillDefinitionId { get; set; }

    /// <summary>
    ///     Whether this agent can execute this skill adaptively.
    /// </summary>
    public required bool CanExecuteAdaptively { get; set; }

    /// <summary>
    ///     Nominal execution duration in seconds.
    /// </summary>
    public required double NominalDuration { get; set; }

    /// <summary>
    ///     Minimum adaptive execution duration in seconds (if adaptive).
    /// </summary>
    public double? MinAdaptiveDuration { get; set; }
}