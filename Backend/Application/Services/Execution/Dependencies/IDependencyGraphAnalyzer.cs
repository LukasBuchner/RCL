using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Services.Execution.Dependencies;

/// <summary>
///     Analyzes domain DependencyEdges to create an event-based dependency graph.
///     Converts edge handles (left=Start, right=Finish) into EventPrerequisites.
///     Supports both TaskNode and SkillExecutionNode hierarchies.
/// </summary>
public interface IDependencyGraphAnalyzer
{
    /// <summary>
    ///     Analyzes nodes and edges to build an event-based dependency graph.
    /// </summary>
    /// <param name="nodes">All nodes in the procedure (TaskNodes and SkillExecutionNodes).</param>
    /// <param name="edges">All dependency edges between nodes.</param>
    /// <returns>A dependency graph mapping skills to their event prerequisites.</returns>
    DependencyGraph AnalyzeDependencies(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges);
}