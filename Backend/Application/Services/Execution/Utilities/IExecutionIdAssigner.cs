using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Utilities;

/// <summary>
///     Assigns unique execution IDs to skill execution nodes.
/// </summary>
public interface IExecutionIdAssigner
{
    /// <summary>
    ///     Assigns unique ExecutionIds to all SkillExecutionNodes in the provided list.
    /// </summary>
    /// <param name="nodes">The list of nodes to process.</param>
    /// <returns>A new list with ExecutionIds assigned to SkillExecutionNodes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null.</exception>
    IReadOnlyList<Node> AssignExecutionIds(IReadOnlyList<Node> nodes);
}