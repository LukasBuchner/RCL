namespace FHOOE.Freydis.Domain.Entities.Variables;

/// <summary>
///     Source of a variable's value.
/// </summary>
public enum VariableSource
{
    UserDefined, // Set by user at execution start
    SkillOutput, // Written by skill completion
    AgentState, // Read from agent metadata
    SensorData, // External system input
    RuntimeComputed // Calculated during execution
}