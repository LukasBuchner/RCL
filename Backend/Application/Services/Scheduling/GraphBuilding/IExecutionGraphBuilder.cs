using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;

/// <summary>
///     Responsible for building an execution graph from procedure nodes and dependency edges.
/// </summary>
public interface IExecutionGraphBuilder
{
    /// <summary>
    ///     Builds an <see cref="IExecutionGraph" /> from the given procedure nodes and dependency edges.
    /// </summary>
    /// <param name="procedureNodes">List of procedure nodes.</param>
    /// <param name="procedureEdges">List of dependency edges.</param>
    /// <param name="durationProvider">Provider for skill duration analysis (planning or execution-aware).</param>
    /// <param name="strictMode">Throw if mapping fails.</param>
    /// <returns>An <see cref="IExecutionGraph" /> or null if building fails.</returns>
    Task<IExecutionGraph?> BuildAsync(
        IReadOnlyList<Node> procedureNodes,
        IReadOnlyList<DependencyEdge> procedureEdges,
        ISkillDurationProvider durationProvider,
        bool strictMode = false);
}