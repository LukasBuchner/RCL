using System.Globalization;
using System.Text.Json;
using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.Agents.Agents.Dummy.Configuration;

/// <summary>
///     Configuration model for creating dummy agents from JSON configuration.
/// </summary>
public class DummyAgentConfiguration
{
    /// <summary>
    ///     Shared position tag definitions that can be referenced by skill properties.
    /// </summary>
    public List<PositionTag>? PositionTags { get; set; }

    /// <summary>
    ///     Shared scene object definitions that can be referenced by skill properties.
    /// </summary>
    public List<SceneObject>? SceneObjects { get; set; }

    /// <summary>
    ///     Shared skill definitions that can be referenced by agents.
    ///     Defines WHAT a skill is (name, description, properties) but NOT performance characteristics.
    /// </summary>
    public List<SkillDefinition>? SkillDefinitions { get; set; }

    /// <summary>
    ///     List of agents to create.
    /// </summary>
    public List<DummyAgentConfig> Agents { get; set; } = [];
}

/// <summary>
///     Defines what a skill IS - its identity, description, and required properties.
///     This is the shared definition that gets synced to the database.
/// </summary>
public class SkillDefinition
{
    /// <summary>
    ///     Unique identifier for the skill.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     Name of the skill.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Description of the skill.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    ///     Properties that define the skill's parameters (e.g., TargetPosition, TargetTag).
    /// </summary>
    public List<DummyPropertyConfig> Properties { get; set; } = [];
}

/// <summary>
///     Configuration for a single dummy agent.
/// </summary>
public class DummyAgentConfig
{
    /// <summary>
    ///     Unique identifier for the agent. If not provided, a new GUID will be generated.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    ///     Name of the agent.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Skills available to this agent.
    /// </summary>
    public List<DummySkillConfig> Skills { get; set; } = [];

    /// <summary>
    ///     Optional description for the agent.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Maximum concurrent executions for this agent (default: 5).
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 5;

    /// <summary>
    ///     Simulated CPU usage range for health monitoring (default: 0-15%).
    /// </summary>
    public CpuUsageRange CpuUsage { get; set; } = new() { Min = 0, Max = 15 };
}

/// <summary>
///     Agent-specific skill configuration that defines HOW an agent performs a skill.
///     References a shared SkillDefinition and adds agent-specific performance characteristics.
/// </summary>
public class DummySkillConfig
{
    /// <summary>
    ///     Reference to a skill definition ID. This defines WHAT the skill is.
    ///     Required when using shared skill definitions.
    /// </summary>
    public Guid? SkillDefinitionId { get; set; }

    /// <summary>
    ///     Direct skill ID (for backward compatibility or inline definitions).
    ///     If provided without SkillDefinitionId, skill must be fully defined inline.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    ///     Inline name (only used if not referencing a SkillDefinition).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Inline description (only used if not referencing a SkillDefinition).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Inline properties (only used if not referencing a SkillDefinition).
    /// </summary>
    public List<DummyPropertyConfig>? Properties { get; set; }

    /// <summary>
    ///     Agent-specific: Whether this agent can execute this skill adaptively.
    /// </summary>
    public required bool CanExecuteAdaptively { get; set; }

    /// <summary>
    ///     Agent-specific: Nominal duration for this agent to execute this skill (seconds).
    /// </summary>
    public required double NominalDuration { get; set; }

    /// <summary>
    ///     Agent-specific: Minimum adaptive duration (seconds). Only for adaptive skills.
    /// </summary>
    public double? MinAdaptiveDuration { get; set; }

    /// <summary>
    ///     Agent-specific: Chance of failure (0.0 to 1.0).
    /// </summary>
    public double FailureChance { get; set; }
}

/// <summary>
///     Configuration for a property of a skill.
/// </summary>
public class DummyPropertyConfig
{
    /// <summary>
    ///     Name of the property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Type of the property (Number, String, Boolean, Position, PositionTag, SceneObject).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    ///     Value of the property (will be converted based on Type).
    ///     For PositionTag/SceneObject, this can be a Guid string (ID reference) or the full object.
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    ///     Optional description for the property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Direction of data flow for the property (Input, Output, InputOutput).
    ///     Defaults to Input if not specified.
    /// </summary>
    public string? Direction { get; set; }
}

/// <summary>
///     Configuration for CPU usage simulation range.
/// </summary>
public class CpuUsageRange
{
    /// <summary>
    ///     Minimum CPU usage percentage.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    ///     Maximum CPU usage percentage.
    /// </summary>
    public double Max { get; set; } = 15;
}

/// <summary>
///     Extension methods for converting configuration to domain entities.
/// </summary>
public static class DummyAgentConfigurationExtensions
{
    /// <summary>
    ///     JSON serializer options for deserializing property values (case-insensitive).
    /// </summary>
    private static readonly JsonSerializerOptions PropertyJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Converts a SkillDefinition to a Domain Skill entity, resolving tag/object references.
    /// </summary>
    public static Skill ToSkill(
        this SkillDefinition definition,
        Dictionary<Guid, PositionTag>? positionTags = null,
        Dictionary<Guid, SceneObject>? sceneObjects = null)
    {
        var properties = definition.Properties.Select(p => p.ToProperty(positionTags, sceneObjects)).ToList();

        return new Skill
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            Properties = properties
        };
    }

    /// <summary>
    ///     Converts a DummySkillConfig (with inline definition) to a Domain Skill entity.
    /// </summary>
    public static Skill ToSkillInline(this DummySkillConfig config)
    {
        var properties = config.Properties?.Select(p => p.ToProperty()).ToList() ?? [];

        // Use description as name if name is not provided, fallback to "Skill"
        var skillName = config.Name ?? config.Description ?? "Skill";
        var description = config.Description ?? config.Name ?? "Skill";

        return new Skill
        {
            Id = config.Id ?? Guid.NewGuid(),
            Name = skillName,
            Description = description,
            Properties = properties
        };
    }

    /// <summary>
    ///     Converts a DummyPropertyConfig to a Domain TypedProperty entity, resolving tag/object references.
    /// </summary>
    public static TypedProperty ToProperty(
        this DummyPropertyConfig config,
        Dictionary<Guid, PositionTag>? positionTags = null,
        Dictionary<Guid, SceneObject>? sceneObjects = null)
    {
        var typedValue = config.Type.ToLowerInvariant() switch
        {
            "number" => CreateNumberTypedValue(config.Value),
            "string" => TypedValue.Text(Convert.ToString(config.Value, CultureInfo.InvariantCulture) ?? ""),
            "boolean" => CreateBooleanTypedValue(config.Value),
            "position" => CreatePositionTypedValue(config.Value),
            "positiontag" => CreatePositionTagTypedValue(config.Value, positionTags),
            "sceneobject" => CreateSceneObjectTypedValue(config.Value, sceneObjects),
            _ => throw new ArgumentException($"Unsupported property type: {config.Type}")
        };

        // Parse direction from config, default to Input if not specified or invalid
        var direction = ParsePropertyDirection(config.Direction);

        return new TypedProperty
        {
            Name = config.Name,
            Value = typedValue,
            Direction = direction
        };
    }

    /// <summary>
    ///     Parses a string direction value to PropertyDirection enum.
    /// </summary>
    private static PropertyDirection ParsePropertyDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return PropertyDirection.Input;

        return direction.ToLowerInvariant() switch
        {
            "input" => PropertyDirection.Input,
            "output" => PropertyDirection.Output,
            "inputoutput" => PropertyDirection.InputOutput,
            _ => PropertyDirection.Input // Default fallback
        };
    }

    /// <summary>
    ///     Creates a TypedValue with NumberType from a JSON value or direct number.
    /// </summary>
    private static TypedValue CreateNumberTypedValue(object value)
    {
        try
        {
            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement) return TypedValue.Number(jsonElement.GetDouble());

            // Handle direct numeric types
            return TypedValue.Number(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to create Number TypedValue: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Creates a TypedValue with BooleanType from a JSON value or direct boolean.
    /// </summary>
    private static TypedValue CreateBooleanTypedValue(object value)
    {
        try
        {
            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement) return TypedValue.Boolean(jsonElement.GetBoolean());

            // Handle direct boolean type
            return TypedValue.Boolean(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to create Boolean TypedValue: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Creates a TypedValue with PositionType from a JSON object value.
    /// </summary>
    private static TypedValue CreatePositionTypedValue(object value)
    {
        try
        {
            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement)
            {
                var position = JsonSerializer.Deserialize<Position>(jsonElement.GetRawText(), PropertyJsonOptions);
                if (position == null)
                    throw new ArgumentException("Failed to deserialize Position from JSON");
                return TypedValue.Position(position);
            }

            // Handle direct Position object
            if (value is Position pos)
                return TypedValue.Position(pos);

            throw new ArgumentException($"Cannot convert value of type {value.GetType()} to Position TypedValue");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to create Position TypedValue: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Creates a TypedValue with PositionTagType from a JSON object value or ID reference.
    /// </summary>
    private static TypedValue CreatePositionTagTypedValue(
        object value,
        Dictionary<Guid, PositionTag>? positionTags = null)
    {
        try
        {
            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement)
            {
                // Try to parse as Guid (ID reference)
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var stringValue = jsonElement.GetString();
                    if (Guid.TryParse(stringValue, out var tagId))
                    {
                        if (positionTags != null && positionTags.TryGetValue(tagId, out var referencedTag))
                            return TypedValue.PositionTag(referencedTag);
                        throw new ArgumentException($"PositionTag with ID {tagId} not found in shared definitions");
                    }
                }

                // Otherwise deserialize as full object
                var positionTag =
                    JsonSerializer.Deserialize<PositionTag>(jsonElement.GetRawText(), PropertyJsonOptions);
                if (positionTag == null)
                    throw new ArgumentException("Failed to deserialize PositionTag from JSON");
                return TypedValue.PositionTag(positionTag);
            }

            // Handle direct PositionTag object
            if (value is PositionTag tag)
                return TypedValue.PositionTag(tag);

            // Handle string (Guid reference)
            if (value is string strValue && Guid.TryParse(strValue, out var guidValue))
            {
                if (positionTags != null && positionTags.TryGetValue(guidValue, out var referencedTag))
                    return TypedValue.PositionTag(referencedTag);
                throw new ArgumentException($"PositionTag with ID {guidValue} not found in shared definitions");
            }

            throw new ArgumentException($"Cannot convert value of type {value.GetType()} to PositionTag TypedValue");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to create PositionTag TypedValue: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Creates a TypedValue with SceneObjectType from a JSON object value or ID reference.
    /// </summary>
    private static TypedValue CreateSceneObjectTypedValue(
        object value,
        Dictionary<Guid, SceneObject>? sceneObjects = null)
    {
        try
        {
            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement)
            {
                // Try to parse as Guid (ID reference)
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var stringValue = jsonElement.GetString();
                    if (Guid.TryParse(stringValue, out var objectId))
                    {
                        if (sceneObjects != null && sceneObjects.TryGetValue(objectId, out var referencedObject))
                            return TypedValue.SceneObject(referencedObject);
                        throw new ArgumentException($"SceneObject with ID {objectId} not found in shared definitions");
                    }
                }

                // Otherwise deserialize as full object
                var sceneObject =
                    JsonSerializer.Deserialize<SceneObject>(jsonElement.GetRawText(), PropertyJsonOptions);
                if (sceneObject == null)
                    throw new ArgumentException("Failed to deserialize SceneObject from JSON");
                return TypedValue.SceneObject(sceneObject);
            }

            // Handle direct SceneObject
            if (value is SceneObject obj)
                return TypedValue.SceneObject(obj);

            // Handle string (Guid reference)
            if (value is string strValue && Guid.TryParse(strValue, out var guidValue))
            {
                if (sceneObjects != null && sceneObjects.TryGetValue(guidValue, out var referencedObject))
                    return TypedValue.SceneObject(referencedObject);
                throw new ArgumentException($"SceneObject with ID {guidValue} not found in shared definitions");
            }

            throw new ArgumentException($"Cannot convert value of type {value.GetType()} to SceneObject TypedValue");
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to create SceneObject TypedValue: {ex.Message}", ex);
        }
    }
}