using System.Text.Json;
using System.Text.Json.Serialization;

namespace FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;

/// <summary>
///     Contains all JSON-serializable message types for the WebSocket communication channel
///     between the Backend and the Unity Digital Twin. Messages are wrapped in a
///     <see cref="DigitalTwinEnvelope" /> containing a type discriminator and a JSON payload.
/// </summary>
public static class DigitalTwinMessages
{
    /// <summary>
    ///     Shared JSON serializer options used for all Digital Twin message serialization.
    ///     Configured for camelCase property naming and case-insensitive deserialization.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    ///     Serializes a message into a <see cref="DigitalTwinEnvelope" /> JSON string.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="messageType">The message type discriminator string from <see cref="MessageTypes" />.</param>
    /// <param name="payload">The message payload to serialize.</param>
    /// <returns>A JSON string containing the envelope with type and serialized payload.</returns>
    public static string Serialize<T>(string messageType, T payload)
    {
        var envelope = new DigitalTwinEnvelope
        {
            Type = messageType,
            Payload = JsonSerializer.SerializeToElement(payload, JsonOptions)
        };
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    /// <summary>
    ///     Deserializes a JSON string into a <see cref="DigitalTwinEnvelope" />,
    ///     allowing the caller to inspect the type and extract the payload.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    ///     The deserialized envelope, or <c>null</c> if the JSON is invalid.
    /// </returns>
    public static DigitalTwinEnvelope? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<DigitalTwinEnvelope>(json, JsonOptions);
    }

    /// <summary>
    ///     Extracts a typed payload from a <see cref="DigitalTwinEnvelope" />.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="envelope">The envelope containing the payload.</param>
    /// <returns>
    ///     The deserialized payload, or <c>default</c> if deserialization fails.
    /// </returns>
    public static T? ExtractPayload<T>(DigitalTwinEnvelope envelope)
    {
        return envelope.Payload.Deserialize<T>(JsonOptions);
    }

    /// <summary>
    ///     All known message type discriminator strings, used for envelope routing.
    /// </summary>
    public static class MessageTypes
    {
        // Backend → Twin
        /// <summary>Command to execute a skill on the Digital Twin.</summary>
        public const string ExecuteSkillCommand = "ExecuteSkillCommand";

        /// <summary>Command to cancel a running skill execution.</summary>
        public const string CancelSkillCommand = "CancelSkillCommand";

        /// <summary>Keepalive ping sent by the Backend.</summary>
        public const string Ping = "Ping";

        /// <summary>Query requesting the Twin to estimate the duration of a skill.</summary>
        public const string EstimateDurationQuery = "EstimateDurationQuery";

        // Twin → Backend
        /// <summary>Registration message sent by the Twin on connection.</summary>
        public const string Register = "Register";

        /// <summary>Progress update for a running skill execution.</summary>
        public const string SkillProgress = "SkillProgress";

        /// <summary>Completion notification for a skill execution.</summary>
        public const string SkillCompleted = "SkillCompleted";

        /// <summary>Keepalive pong reply from the Twin.</summary>
        public const string Pong = "Pong";

        /// <summary>Health status report from the Twin.</summary>
        public const string HealthStatus = "HealthStatus";

        /// <summary>Duration estimation response from the Twin.</summary>
        public const string EstimateDurationResponse = "EstimateDurationResponse";
    }
}

/// <summary>
///     Wrapper envelope for all Digital Twin WebSocket messages.
///     Contains a type discriminator and a JSON payload element.
/// </summary>
public class DigitalTwinEnvelope
{
    /// <summary>
    ///     The message type discriminator string.
    ///     Must match one of the constants in <see cref="DigitalTwinMessages.MessageTypes" />.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    ///     The message payload as a raw JSON element.
    ///     Deserialized to a specific type based on <see cref="Type" />.
    /// </summary>
    public JsonElement Payload { get; set; }
}

// ─── Backend → Twin messages ────────────────────────────────────────

/// <summary>
///     Command sent from Backend to Digital Twin to execute a skill.
///     The Twin should start the skill execution and report progress back.
/// </summary>
public class ExecuteSkillCommand
{
    /// <summary>
    ///     Unique identifier for this execution instance, assigned by the orchestrator.
    ///     Used to correlate progress and completion messages.
    /// </summary>
    public required Guid ExecutionId { get; set; }

    /// <summary>
    ///     Name of the skill to execute (e.g., "Move To Position").
    /// </summary>
    public required string SkillName { get; set; }

    /// <summary>
    ///     Target parameters for the skill execution, containing the 6-DOF pose.
    /// </summary>
    public required SkillParameters Parameters { get; set; }

    /// <summary>
    ///     Optional IK mode override. If <c>null</c>, the Twin uses its current configuration.
    ///     Valid values: "ArticulationBody", "Kinematic".
    /// </summary>
    public string? IkMode { get; set; }
}

/// <summary>
///     Target pose parameters for a skill execution, representing a 6-DOF Cartesian pose
///     and optional hold duration. Position fields are used by movement skills;
///     <see cref="Duration" /> is used by the Hold Position skill.
/// </summary>
public class SkillParameters
{
    /// <summary>X position in metres.</summary>
    public double X { get; set; }

    /// <summary>Y position in metres.</summary>
    public double Y { get; set; }

    /// <summary>Z position in metres.</summary>
    public double Z { get; set; }

    /// <summary>Rotation around the X axis in degrees.</summary>
    public double Alpha { get; set; }

    /// <summary>Rotation around the Y axis in degrees.</summary>
    public double Beta { get; set; }

    /// <summary>Rotation around the Z axis in degrees.</summary>
    public double Gamma { get; set; }

    /// <summary>
    ///     Hold duration in seconds. Used by the Hold Position skill to specify
    ///     how long the robot should maintain its current pose. Null for movement skills.
    ///     Omitted from JSON when null (via <see cref="DigitalTwinMessages.JsonOptions" />).
    /// </summary>
    public double? Duration { get; set; }
}

/// <summary>
///     Command sent from Backend to Digital Twin to cancel an in-progress skill execution.
/// </summary>
public class CancelSkillCommand
{
    /// <summary>
    ///     The execution ID of the skill to cancel. Must match a previously sent
    ///     <see cref="ExecuteSkillCommand.ExecutionId" />.
    /// </summary>
    public required Guid ExecutionId { get; set; }
}

/// <summary>
///     Keepalive ping message sent from Backend to Digital Twin.
///     The Twin should respond with a <see cref="PongMessage" />.
/// </summary>
public class PingCommand
{
    /// <summary>
    ///     Timestamp of when the ping was sent, in UTC.
    ///     Echoed back in the pong for round-trip time calculation.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     Query sent from Backend to Digital Twin requesting a duration estimate for a skill.
///     The Twin should compute the estimate and reply with <see cref="EstimateDurationResponse" />.
/// </summary>
public class EstimateDurationQuery
{
    /// <summary>
    ///     Name of the skill to estimate (e.g., "Move To Position").
    /// </summary>
    public required string SkillName { get; set; }

    /// <summary>
    ///     Target parameters for the estimation, containing the 6-DOF target pose.
    /// </summary>
    public required SkillParameters Parameters { get; set; }

    /// <summary>
    ///     Correlation ID for matching query to response.
    /// </summary>
    public Guid QueryId { get; set; } = Guid.NewGuid();
}

// ─── Twin → Backend messages ────────────────────────────────────────

/// <summary>
///     Registration message sent by the Digital Twin immediately after connecting via WebSocket.
///     Contains the agent identity and the list of skills the Twin can execute.
/// </summary>
public class RegisterMessage
{
    /// <summary>
    ///     Optional stable agent ID. When provided, the agent reuses this ID across reconnections
    ///     so workflows and domain records persist. When omitted or empty, a deterministic ID is
    ///     generated from the <see cref="AgentName" /> so that reconnections with the same name
    ///     still match. Uses <see cref="LenientNullableGuidConverter" /> to tolerate empty strings
    ///     sent by Unity's JsonUtility.
    /// </summary>
    [JsonConverter(typeof(LenientNullableGuidConverter))]
    public Guid? AgentId { get; set; }

    /// <summary>
    ///     Display name for this Digital Twin agent (e.g., "DigitalTwin-IIWA-01").
    /// </summary>
    public required string AgentName { get; set; }

    /// <summary>
    ///     List of skill definition IDs that this Digital Twin can execute.
    ///     These IDs must match entries in the Backend's skills-config.json.
    /// </summary>
    public List<Guid> AvailableSkillIds { get; set; } = [];
}

/// <summary>
///     Progress update sent from Digital Twin to Backend during skill execution.
///     Reports the current convergence progress as a percentage.
/// </summary>
public class SkillProgressMessage
{
    /// <summary>
    ///     The execution ID this progress update relates to.
    /// </summary>
    public required Guid ExecutionId { get; set; }

    /// <summary>
    ///     Overall progress as a value between 0.0 and 1.0.
    ///     Computed as <c>min(posProgress, rotProgress)</c> where each is
    ///     <c>1 - (currentError / initialError)</c>.
    /// </summary>
    public double ProgressPercent { get; set; }

    /// <summary>
    ///     Human-readable status message describing current execution state.
    /// </summary>
    public string StatusMessage { get; set; } = "";
}

/// <summary>
///     Completion message sent from Digital Twin to Backend when a skill execution finishes.
/// </summary>
public class SkillCompletedMessage
{
    /// <summary>
    ///     The execution ID of the completed skill.
    /// </summary>
    public required Guid ExecutionId { get; set; }

    /// <summary>
    ///     Whether the skill completed successfully (convergence reached within tolerance).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Error message if the execution failed (e.g., "Timeout", "Joint limit reached").
    ///     Null when <see cref="Success" /> is <c>true</c>.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     Keepalive pong reply from Digital Twin in response to a <see cref="PingCommand" />.
/// </summary>
public class PongMessage
{
    /// <summary>
    ///     Echoed timestamp from the original <see cref="PingCommand" />,
    ///     allowing the Backend to compute round-trip latency.
    /// </summary>
    public DateTime OriginalTimestampUtc { get; set; }
}

/// <summary>
///     Periodic health status report sent from Digital Twin to Backend.
///     Provides resource usage and uptime information.
/// </summary>
public class HealthStatusMessage
{
    /// <summary>Current CPU usage percentage on the Twin host (0-100).</summary>
    public double CpuPercent { get; set; }

    /// <summary>Current memory usage in megabytes on the Twin host.</summary>
    public double MemoryMb { get; set; }

    /// <summary>Time in seconds since the Twin application started.</summary>
    public double UptimeSeconds { get; set; }

    /// <summary>Current frames per second in the Unity simulation.</summary>
    public double Fps { get; set; }
}

/// <summary>
///     Duration estimation response from Digital Twin in reply to an <see cref="EstimateDurationQuery" />.
///     Contains the computed motion time based on Cartesian distance and velocity limits.
/// </summary>
public class EstimateDurationResponse
{
    /// <summary>
    ///     Correlation ID matching the original <see cref="EstimateDurationQuery.QueryId" />.
    /// </summary>
    public Guid QueryId { get; set; }

    /// <summary>
    ///     Best estimate of the total execution duration in seconds.
    ///     Computed as <c>max(posDistance/linearSpeed, angDistance/angularSpeed) + convergenceOverhead</c>.
    /// </summary>
    public double EstimatedDurationSeconds { get; set; }

    /// <summary>
    ///     Minimum possible duration (no convergence overhead). Null if not computed.
    /// </summary>
    public double? MinDuration { get; set; }
}

/// <summary>
///     JSON converter for <see cref="Guid" />? that treats empty or whitespace strings as <c>null</c>
///     instead of throwing. Required because Unity's <c>JsonUtility</c> serializes null strings
///     as <c>""</c> and cannot omit fields conditionally.
/// </summary>
public class LenientNullableGuidConverter : JsonConverter<Guid?>
{
    /// <inheritdoc />
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Guid.Parse(value);
        }

        // Default System.Text.Json GUID reading for non-string tokens
        return reader.GetGuid();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}