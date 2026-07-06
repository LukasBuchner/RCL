using FHOOE.Freydis.Application.Services.Execution.StateManagement;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.StateManagement;

/// <summary>
///     Unit tests for <see cref="ExecutionStatusExtensions.IsTerminal" /> covering the
///     truth table mirrored from the Lean predicate at
///     <c>Sunstone/Sunstone/Common/ExecutionStatus.lean:29</c>.
/// </summary>
public class ExecutionStatusExtensionsTests
{
    [Fact]
    public void IsTerminal_Completed_ReturnsTrue()
    {
        Assert.True(ExecutionStatus.Completed.IsTerminal());
    }

    [Fact]
    public void IsTerminal_Failed_ReturnsTrue()
    {
        Assert.True(ExecutionStatus.Failed.IsTerminal());
    }

    [Fact]
    public void IsTerminal_NotSelected_ReturnsTrue()
    {
        Assert.True(ExecutionStatus.NotSelected.IsTerminal());
    }

    [Fact]
    public void IsTerminal_NotStarted_ReturnsFalse()
    {
        Assert.False(ExecutionStatus.NotStarted.IsTerminal());
    }

    [Fact]
    public void IsTerminal_Scheduled_ReturnsFalse()
    {
        Assert.False(ExecutionStatus.Scheduled.IsTerminal());
    }

    [Fact]
    public void IsTerminal_Running_ReturnsFalse()
    {
        Assert.False(ExecutionStatus.Running.IsTerminal());
    }
}