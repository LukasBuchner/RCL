using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Tests.Services.Common;

/// <summary>
///     Tests for <see cref="HandleDependencyTypeMapper" />, covering the handle-to-event mapping
///     and the canonical and tolerant handle-pair-to-dependency-type mapping.
/// </summary>
public sealed class HandleDependencyTypeMapperTests
{
    /// <summary>
    ///     "left" maps to Start, "right" to Finish, and any other value, including null, empty,
    ///     whitespace, or unknown tokens, maps to Finish.
    /// </summary>
    [Theory]
    [InlineData("left", EventTriggerType.Start)]
    [InlineData("right", EventTriggerType.Finish)]
    [InlineData("LEFT", EventTriggerType.Start)]
    [InlineData("  right  ", EventTriggerType.Finish)]
    [InlineData("foo", EventTriggerType.Finish)]
    [InlineData("", EventTriggerType.Finish)]
    [InlineData("   ", EventTriggerType.Finish)]
    [InlineData(null, EventTriggerType.Finish)]
    public void ToEventType_MapsHandleToEventType(string? handle, EventTriggerType expected)
    {
        Assert.Equal(expected, HandleDependencyTypeMapper.ToEventType(handle));
    }

    /// <summary>
    ///     The four canonical handle pairs map to their corresponding dependency types.
    /// </summary>
    [Theory]
    [InlineData("right", "left", DependencyType.FinishToStart)]
    [InlineData("left", "left", DependencyType.StartToStart)]
    [InlineData("left", "right", DependencyType.StartToFinish)]
    [InlineData("right", "right", DependencyType.FinishToFinish)]
    public void ToDependencyType_CanonicalPairs_ReturnsExpected(string src, string tgt,
        DependencyType expected)
    {
        Assert.Equal(expected, HandleDependencyTypeMapper.ToDependencyType(src, tgt));
    }

    /// <summary>
    ///     Unrecognized or null handles are coerced to Finish endpoints, so the mapping stays total
    ///     and never throws.
    /// </summary>
    [Theory]
    [InlineData("foo", "bar", DependencyType.FinishToFinish)]
    [InlineData(null, "left", DependencyType.FinishToStart)]
    [InlineData("right", null, DependencyType.FinishToFinish)]
    [InlineData(null, null, DependencyType.FinishToFinish)]
    [InlineData("left", null, DependencyType.StartToFinish)]
    public void ToDependencyType_UnrecognizedOrNullHandles_ReturnsTolerantMapping(string? src,
        string? tgt, DependencyType expected)
    {
        Assert.Equal(expected, HandleDependencyTypeMapper.ToDependencyType(src, tgt));
    }
}