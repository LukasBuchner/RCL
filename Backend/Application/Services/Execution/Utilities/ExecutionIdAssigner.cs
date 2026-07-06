using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Utilities;

/// <summary>
///     Assigns unique execution IDs to skill execution nodes.
/// </summary>
public class ExecutionIdAssigner : IExecutionIdAssigner
{
    /// <inheritdoc />
    public IReadOnlyList<Node> AssignExecutionIds(IReadOnlyList<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var updatedNodes = new List<Node>();

        foreach (var node in nodes)
            if (node is SkillExecutionNode skillNode)
            {
                var executionId = Guid.NewGuid();
                var updatedTask = skillNode.SkillExecutionTask with { ExecutionId = executionId };
                var updatedSkillNode = skillNode with { SkillExecutionTask = updatedTask };
                updatedNodes.Add(updatedSkillNode);
            }
            else
            {
                updatedNodes.Add(node);
            }

        return updatedNodes.AsReadOnly();
    }
}