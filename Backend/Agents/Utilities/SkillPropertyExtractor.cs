using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Agents.Utilities;

/// <summary>
///     Extracts typed property values from skill definitions for use in agent operations.
/// </summary>
public sealed class SkillPropertyExtractor
{
    private readonly ILogger<SkillPropertyExtractor> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SkillPropertyExtractor" /> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SkillPropertyExtractor(ILogger<SkillPropertyExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Extracts 6DOF pose parameters (x, y, z, alpha, beta, gamma) from a skill's properties.
    ///     Supports both PositionType (with nested Position object) and individual NumberType values.
    ///     Used to extract target positions for robot motion commands.
    /// </summary>
    /// <param name="skill">The skill containing pose properties.</param>
    /// <returns>A tuple containing the 6DOF pose: (x, y, z, alpha, beta, gamma). Uses 0.0 for missing properties.</returns>
    public (double x, double y, double z, double alpha, double beta, double gamma) ExtractPose(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        // First, check for PositionType (e.g., "TargetPosition") which contains all 6DOF values
        var positionProperty = skill.Properties.FirstOrDefault(p => p.Value.Type is PositionType);
        if (positionProperty?.Value.Value is Position position)
        {
            _logger.LogPoseExtracted(
                positionProperty.Name, skill.Name, position.X, position.Y, position.Z, position.Alpha, position.Beta,
                position.Gamma);

            return (position.X, position.Y, position.Z, position.Alpha, position.Beta, position.Gamma);
        }

        // Fallback: Extract individual NumberType values for backward compatibility
        double GetPropertyValue(string propertyName, double defaultValue = 0.0)
        {
            var property = skill.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property?.Value is { Type: NumberType, Value: double numValue }) return numValue;

            _logger.LogPropertyNotFoundOrNotNumber(propertyName, skill.Name, defaultValue);
            return defaultValue;
        }

        return (
            x: GetPropertyValue("X"),
            y: GetPropertyValue("Y"),
            z: GetPropertyValue("Z"),
            alpha: GetPropertyValue("Alpha"),
            beta: GetPropertyValue("Beta"),
            gamma: GetPropertyValue("Gamma")
        );
    }
}