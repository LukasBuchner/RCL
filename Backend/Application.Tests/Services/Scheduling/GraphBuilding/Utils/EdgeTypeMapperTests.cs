using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.GraphBuilding.Utils;

/// <summary>
///     Unit tests for <see cref="EdgeTypeMapper" /> covering the canonical handle‐pairs and the
///     tolerant mapping of unrecognized or null handles.
/// </summary>
public class EdgeTypeMapperTests
{
    private readonly EdgeTypeMapper _mapper = new();

    /// <summary>
    ///     Valid handle combinations map to their <see cref="DependencyType" />.
    /// </summary>
    [Theory]
    [InlineData("right", "left", DependencyType.FinishToStart)]
    [InlineData("left", "left", DependencyType.StartToStart)]
    [InlineData("left", "right", DependencyType.StartToFinish)]
    [InlineData("right", "right", DependencyType.FinishToFinish)]
    public void Map_ValidPairs_ReturnsExpected(string src, string tgt, DependencyType expected)
    {
        var edge = new DependencyEdge
        {
            SourceHandle = src,
            TargetHandle = tgt,
            Id = Guid.Empty,
            SourceId = Guid.Empty,
            TargetId = Guid.Empty,
            ProcedureId = default
        };
        var actual = _mapper.Map(edge);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Unrecognized or null handles are tolerated and coerced to Finish endpoints rather than
    ///     throwing, so edges carrying such handles still map to a <see cref="DependencyType" />.
    /// </summary>
    [Theory]
    [InlineData("foo", "bar", DependencyType.FinishToFinish)]
    [InlineData(null, "left", DependencyType.FinishToStart)]
    [InlineData("right", null, DependencyType.FinishToFinish)]
    [InlineData(null, null, DependencyType.FinishToFinish)]
    public void Map_UnrecognizedOrNullHandles_ReturnsTolerantMapping(string? src, string? tgt,
        DependencyType expected)
    {
        var edge = new DependencyEdge
        {
            SourceHandle = src,
            TargetHandle = tgt,
            Id = Guid.Empty,
            SourceId = Guid.Empty,
            TargetId = Guid.Empty,
            ProcedureId = default
        };
        var actual = _mapper.Map(edge);
        Assert.Equal(expected, actual);
    }
}