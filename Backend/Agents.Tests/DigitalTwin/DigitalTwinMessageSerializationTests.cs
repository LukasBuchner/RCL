using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Protocol;

namespace FHOOE.Freydis.Agents.Tests.DigitalTwin;

/// <summary>
///     Tests JSON serialization/deserialization round-trips for all Digital Twin message types,
///     envelope wrapping/unwrapping, and graceful handling of unknown message types.
/// </summary>
public class DigitalTwinMessageSerializationTests
{
    [Fact]
    public void Serialize_ExecuteSkillCommand_RoundTrips()
    {
        // Arrange
        var command = new ExecuteSkillCommand
        {
            ExecutionId = Guid.NewGuid(),
            SkillName = "Move To Position",
            Parameters = new SkillParameters { X = 1.0, Y = 2.5, Z = 0.3, Alpha = 45, Beta = 0, Gamma = 90 },
            IkMode = "ArticulationBody"
        };

        // Act
        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command);
        var envelope = DigitalTwinMessages.Deserialize(json);

        // Assert
        Assert.NotNull(envelope);
        Assert.Equal(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, envelope!.Type);

        var deserialized = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(envelope);
        Assert.NotNull(deserialized);
        Assert.Equal(command.ExecutionId, deserialized!.ExecutionId);
        Assert.Equal(command.SkillName, deserialized.SkillName);
        Assert.Equal(command.Parameters.X, deserialized.Parameters.X);
        Assert.Equal(command.Parameters.Y, deserialized.Parameters.Y);
        Assert.Equal(command.Parameters.Z, deserialized.Parameters.Z);
        Assert.Equal(command.Parameters.Alpha, deserialized.Parameters.Alpha);
        Assert.Equal("ArticulationBody", deserialized.IkMode);
    }

    [Fact]
    public void Serialize_ExecuteSkillCommand_IkModeNull_OmittedInJson()
    {
        // Arrange
        var command = new ExecuteSkillCommand
        {
            ExecutionId = Guid.NewGuid(),
            SkillName = "Move To Position",
            Parameters = new SkillParameters(),
            IkMode = null
        };

        // Act
        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command);

        // Assert — null fields should be omitted (WhenWritingNull)
        Assert.DoesNotContain("ikMode", json);

        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(envelope!);
        Assert.Null(deserialized!.IkMode);
    }

    [Fact]
    public void Serialize_CancelSkillCommand_RoundTrips()
    {
        var executionId = Guid.NewGuid();
        var command = new CancelSkillCommand { ExecutionId = executionId };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.CancelSkillCommand, command);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<CancelSkillCommand>(envelope!);

        Assert.Equal(executionId, deserialized!.ExecutionId);
    }

    [Fact]
    public void Serialize_RegisterMessage_RoundTrips()
    {
        var message = new RegisterMessage
        {
            AgentName = "DigitalTwin-IIWA-01",
            AvailableSkillIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.Register, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<RegisterMessage>(envelope!);

        Assert.Equal("DigitalTwin-IIWA-01", deserialized!.AgentName);
        Assert.Equal(2, deserialized.AvailableSkillIds.Count);
    }

    [Fact]
    public void Serialize_SkillProgressMessage_RoundTrips()
    {
        var executionId = Guid.NewGuid();
        var message = new SkillProgressMessage
        {
            ExecutionId = executionId,
            ProgressPercent = 0.75,
            StatusMessage = "Moving to target position"
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.SkillProgress, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<SkillProgressMessage>(envelope!);

        Assert.Equal(executionId, deserialized!.ExecutionId);
        Assert.Equal(0.75, deserialized.ProgressPercent);
        Assert.Equal("Moving to target position", deserialized.StatusMessage);
    }

    [Fact]
    public void Serialize_SkillCompletedMessage_Success_RoundTrips()
    {
        var message = new SkillCompletedMessage
        {
            ExecutionId = Guid.NewGuid(),
            Success = true,
            ErrorMessage = null
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.SkillCompleted, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<SkillCompletedMessage>(envelope!);

        Assert.True(deserialized!.Success);
        Assert.Null(deserialized.ErrorMessage);
    }

    [Fact]
    public void Serialize_SkillCompletedMessage_Failure_RoundTrips()
    {
        var message = new SkillCompletedMessage
        {
            ExecutionId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "Timeout after 30s"
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.SkillCompleted, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<SkillCompletedMessage>(envelope!);

        Assert.False(deserialized!.Success);
        Assert.Equal("Timeout after 30s", deserialized.ErrorMessage);
    }

    [Fact]
    public void Serialize_PingCommand_RoundTrips()
    {
        var now = DateTime.UtcNow;
        var command = new PingCommand { TimestampUtc = now };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.Ping, command);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<PingCommand>(envelope!);

        Assert.Equal(now.ToString("O"), deserialized!.TimestampUtc.ToString("O"));
    }

    [Fact]
    public void Serialize_PongMessage_RoundTrips()
    {
        var ts = DateTime.UtcNow.AddSeconds(-1);
        var message = new PongMessage { OriginalTimestampUtc = ts };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.Pong, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<PongMessage>(envelope!);

        Assert.Equal(ts.ToString("O"), deserialized!.OriginalTimestampUtc.ToString("O"));
    }

    [Fact]
    public void Serialize_HealthStatusMessage_RoundTrips()
    {
        var message = new HealthStatusMessage
        {
            CpuPercent = 42.5,
            MemoryMb = 1024,
            UptimeSeconds = 3600,
            Fps = 72.0
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.HealthStatus, message);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<HealthStatusMessage>(envelope!);

        Assert.Equal(42.5, deserialized!.CpuPercent);
        Assert.Equal(1024, deserialized.MemoryMb);
        Assert.Equal(3600, deserialized.UptimeSeconds);
        Assert.Equal(72.0, deserialized.Fps);
    }

    [Fact]
    public void Serialize_EstimateDurationQuery_RoundTrips()
    {
        var queryId = Guid.NewGuid();
        var query = new EstimateDurationQuery
        {
            SkillName = "Move To Position",
            Parameters = new SkillParameters { X = 0.5, Y = 0.3, Z = 0.8 },
            QueryId = queryId
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.EstimateDurationQuery, query);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<EstimateDurationQuery>(envelope!);

        Assert.Equal("Move To Position", deserialized!.SkillName);
        Assert.Equal(queryId, deserialized.QueryId);
        Assert.Equal(0.5, deserialized.Parameters.X);
    }

    [Fact]
    public void Serialize_EstimateDurationResponse_RoundTrips()
    {
        var queryId = Guid.NewGuid();
        var response = new EstimateDurationResponse
        {
            QueryId = queryId,
            EstimatedDurationSeconds = 3.5,
            MinDuration = 3.0
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.EstimateDurationResponse, response);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<EstimateDurationResponse>(envelope!);

        Assert.Equal(queryId, deserialized!.QueryId);
        Assert.Equal(3.5, deserialized.EstimatedDurationSeconds);
        Assert.Equal(3.0, deserialized.MinDuration);
    }

    [Fact]
    public void Deserialize_UnknownMessageType_ReturnsEnvelopeWithType()
    {
        var json = """{"type":"FutureMessageType","payload":{"foo":"bar"}}""";

        var envelope = DigitalTwinMessages.Deserialize(json);

        Assert.NotNull(envelope);
        Assert.Equal("FutureMessageType", envelope!.Type);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => DigitalTwinMessages.Deserialize("not json at all"));
    }

    [Fact]
    public void Serialize_SkillParameters_Duration_RoundTrips()
    {
        // Arrange — Hold Position command with Duration
        var command = new ExecuteSkillCommand
        {
            ExecutionId = Guid.NewGuid(),
            SkillName = "Hold Position",
            Parameters = new SkillParameters { Duration = 10.0 }
        };

        // Act
        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command);
        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(envelope!);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("Hold Position", deserialized!.SkillName);
        Assert.Equal(10.0, deserialized.Parameters.Duration);
        Assert.Equal(0.0, deserialized.Parameters.X);
    }

    [Fact]
    public void Serialize_SkillParameters_DurationNull_OmittedInJson()
    {
        // Arrange — movement command without Duration
        var command = new ExecuteSkillCommand
        {
            ExecutionId = Guid.NewGuid(),
            SkillName = "Move To Position",
            Parameters = new SkillParameters { X = 1.0, Y = 2.0, Z = 3.0, Duration = null }
        };

        // Act
        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command);

        // Assert — null Duration should be omitted (WhenWritingNull)
        Assert.DoesNotContain("duration", json);

        var envelope = DigitalTwinMessages.Deserialize(json);
        var deserialized = DigitalTwinMessages.ExtractPayload<ExecuteSkillCommand>(envelope!);
        Assert.Null(deserialized!.Parameters.Duration);
    }

    [Fact]
    public void Envelope_UsesCamelCase()
    {
        var command = new ExecuteSkillCommand
        {
            ExecutionId = Guid.NewGuid(),
            SkillName = "Test",
            Parameters = new SkillParameters()
        };

        var json = DigitalTwinMessages.Serialize(DigitalTwinMessages.MessageTypes.ExecuteSkillCommand, command);

        Assert.Contains("\"type\"", json);
        Assert.Contains("\"payload\"", json);
        Assert.Contains("\"executionId\"", json);
        Assert.Contains("\"skillName\"", json);
    }
}