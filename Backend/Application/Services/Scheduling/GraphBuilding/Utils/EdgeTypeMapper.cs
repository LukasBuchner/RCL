using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;

/// <summary>
///     Default implementation of <see cref="IEdgeTypeMapper" /> converting handle‐pairs
///     into <see cref="DependencyType" /> values.
/// </summary>
public class EdgeTypeMapper : IEdgeTypeMapper
{
    /// <inheritdoc />
    public DependencyType Map(DependencyEdge edge)
    {
        return HandleDependencyTypeMapper.ToDependencyType(edge.SourceHandle, edge.TargetHandle);
    }
}