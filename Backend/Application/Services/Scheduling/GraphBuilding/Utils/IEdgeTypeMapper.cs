using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;

/// <summary>
///     Converts a <see cref="DependencyEdge" /> into a scheduling <see cref="DependencyType" />.
/// </summary>
public interface IEdgeTypeMapper
{
    /// <summary>
    ///     Maps the directional handles on <paramref name="edge" /> to a <see cref="DependencyType" />.
    ///     Handles are matched case-insensitively after trimming: <c>"left"</c> denotes a Start endpoint
    ///     and <c>"right"</c> a Finish endpoint. Any unrecognized handle, including <see langword="null" />,
    ///     empty, or whitespace, is treated as Finish, so the mapping is total and never throws.
    /// </summary>
    /// <param name="edge">The dependency edge containing source/target handles.</param>
    /// <returns>The corresponding <see cref="DependencyType" /> for the (source, target) handle pair.</returns>
    DependencyType Map(DependencyEdge edge);
}