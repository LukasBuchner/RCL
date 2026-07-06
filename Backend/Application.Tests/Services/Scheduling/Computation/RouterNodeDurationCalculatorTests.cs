using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Computation;

/// <summary>
///     Unit tests for <see cref="RouterNodeDurationCalculator" />.
///     Verifies duration and schedule calculations for selected branches, no-selection (max of branches),
///     and fallback scenarios.
/// </summary>
public class RouterNodeDurationCalculatorTests
{
    private readonly RouterNodeDurationCalculator _calculator;
    private readonly Mock<ILogger<RouterNodeDurationCalculator>> _mockLogger;

    public RouterNodeDurationCalculatorTests()
    {
        _mockLogger = new Mock<ILogger<RouterNodeDurationCalculator>>();
        _calculator = new RouterNodeDurationCalculator(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RouterNodeDurationCalculator(null!));
    }

    #endregion

    #region CalculateRouterNodeSchedules - Multiple Routers Tests

    [Fact]
    public void CalculateRouterNodeSchedules_MultipleRouters_MixedSelections_ShouldHandleEachIndependently()
    {
        // Arrange
        var r1BranchATargetId = Guid.NewGuid();
        var r1BranchBTargetId = Guid.NewGuid();
        var r2BranchATargetId = Guid.NewGuid();

        var router1 = CreateRouterNode("Router1-NoSelection", 5.0, branches:
        [
            CreateBranch("R1-A", r1BranchATargetId),
            CreateBranch("R1-B", r1BranchBTargetId)
        ]);

        var router2 = CreateRouterNode("Router2-Selected", 3.0, branches:
        [
            CreateBranch("R2-A", r2BranchATargetId)
        ], selectedBranchTargetNodeId: r2BranchATargetId);

        var allNodes = new List<Node> { router1, router2 };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [r1BranchATargetId] = (8.0, 0.0, 8.0),
            [r1BranchBTargetId] = (15.0, 0.0, 15.0),
            [r2BranchATargetId] = (20.0, 5.0, 25.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert
        Assert.Equal(2, result.Count);

        // Router1: no selection → max of branches = 15
        var r1Schedule = result[router1.Id];
        Assert.Equal(15.0, r1Schedule.Duration);
        Assert.Equal(0.0, r1Schedule.StartTime);
        Assert.Equal(15.0, r1Schedule.FinishTime);

        // Router2: selected branch → uses that branch
        var r2Schedule = result[router2.Id];
        Assert.Equal(20.0, r2Schedule.Duration);
        Assert.Equal(5.0, r2Schedule.StartTime);
        Assert.Equal(25.0, r2Schedule.FinishTime);
    }

    #endregion

    #region CalculateRouterNodeDuration - No Selection Tests

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_WithBranchTimings_ShouldReturnMaxDuration()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - should be 12 (max), not the stale 5.0
        Assert.Equal(12.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_WithDifferentStartTimes_ShouldReturnSpanDuration()
    {
        // Arrange: BranchA starts at 2, ends at 8. BranchB starts at 0, ends at 10.
        // Span = 10 - 0 = 10.
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 3.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (6.0, 2.0, 8.0),
            [branchBTargetId] = (10.0, 0.0, 10.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - span from earliest start (0) to latest finish (10) = 10
        Assert.Equal(10.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_NoBranches_ShouldReturnNull()
    {
        // Arrange
        var router = CreateRouterNode("Router1", 5.0, branches: []);
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_BranchesWithNoTimings_ShouldReturnNull()
    {
        // Arrange - branches exist but none have timing data
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", Guid.NewGuid()),
            CreateBranch("BranchB", Guid.NewGuid())
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_PartialBranchTimings_ShouldUseAvailableBranches()
    {
        // Arrange - only one of two branches has timing data
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0)
            // branchBTargetId has no timing
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - uses the one branch that has timing
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_SingleBranch_ShouldReturnThatBranchDuration()
    {
        // Arrange
        var branchTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("OnlyBranch", branchTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchTargetId] = (20.0, 5.0, 25.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert
        Assert.Equal(20.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_NoSelection_BranchWithNullTargetNodeId_ShouldSkipIt()
    {
        // Arrange - one branch has no target, the other does
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", null),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchBTargetId] = (15.0, 0.0, 15.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert
        Assert.Equal(15.0, result);
    }

    #endregion

    #region CalculateRouterNodeDuration - Selected Branch Tests

    [Fact]
    public void CalculateRouterNodeDuration_ExecutionSelection_ShouldUseSelectedBranch()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        var routerSelections = new Dictionary<Guid, Guid> { [router.Id] = branchATargetId };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(
            router, new List<Node> { router }, nodeTimings, routerSelections);

        // Assert - uses selected branch A (8), not max (12)
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_ExecutionStateSelection_ShouldUseSelectedBranch()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ], selectedBranchTargetNodeId: branchATargetId);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - uses execution state selected branch A (8), not max (12)
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_ManualSelection_ShouldUseSelectedBranch()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ], manuallySelectedBranch: "BranchB");

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - uses manually selected branch B (12)
        Assert.Equal(12.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_SelectedBranchWithNoTiming_ShouldReturnNull()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId)
        ], selectedBranchTargetNodeId: branchATargetId);

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CalculateRouterNodeDuration - Argument Validation Tests

    [Fact]
    public void CalculateRouterNodeDuration_NullRouterNode_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateRouterNodeDuration(null!, new List<Node>(),
                new Dictionary<Guid, (double, double, double)>()));
    }

    [Fact]
    public void CalculateRouterNodeDuration_NullAllNodes_ShouldThrow()
    {
        var router = CreateRouterNode("R", 1.0);
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateRouterNodeDuration(router, null!,
                new Dictionary<Guid, (double, double, double)>()));
    }

    [Fact]
    public void CalculateRouterNodeDuration_NullNodeTimings_ShouldThrow()
    {
        var router = CreateRouterNode("R", 1.0);
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateRouterNodeDuration(router, new List<Node>(), null!));
    }

    #endregion

    #region CalculateRouterNodeSchedules - No Selection Tests

    [Fact]
    public void CalculateRouterNodeSchedules_NoSelection_WithBranchTimings_ShouldUseMaxBranchSchedule()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert - should span from 0 to 12, not use original 5.0
        Assert.Single(result);
        var schedule = result[router.Id];
        Assert.Equal(12.0, schedule.Duration);
        Assert.Equal(0.0, schedule.StartTime);
        Assert.Equal(12.0, schedule.FinishTime);
    }

    [Fact]
    public void CalculateRouterNodeSchedules_NoSelection_WithDifferentStartTimes_ShouldSpanAllBranches()
    {
        // Arrange: BranchA [2, 8], BranchB [0, 10], BranchC [4, 7]
        // Expected span: [0, 10], duration = 10
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var branchCTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 1.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId),
            CreateBranch("BranchC", branchCTargetId)
        ]);

        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (6.0, 2.0, 8.0),
            [branchBTargetId] = (10.0, 0.0, 10.0),
            [branchCTargetId] = (3.0, 4.0, 7.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert
        var schedule = result[router.Id];
        Assert.Equal(10.0, schedule.Duration);
        Assert.Equal(0.0, schedule.StartTime);
        Assert.Equal(10.0, schedule.FinishTime);
    }

    [Fact]
    public void CalculateRouterNodeSchedules_NoSelection_NoBranchTimings_ShouldFallBackToOriginal()
    {
        // Arrange - branches exist but no timings available
        var router = CreateRouterNode("Router1", 5.0, 3.0, [
            CreateBranch("BranchA", Guid.NewGuid())
        ]);

        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert - falls back to original stored values
        var schedule = result[router.Id];
        Assert.Equal(5.0, schedule.Duration);
        Assert.Equal(3.0, schedule.StartTime);
        Assert.Equal(8.0, schedule.FinishTime);
    }

    [Fact]
    public void CalculateRouterNodeSchedules_NoSelection_NoBranches_ShouldFallBackToOriginal()
    {
        // Arrange
        var router = CreateRouterNode("Router1", 7.0, 2.0, []);
        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert
        var schedule = result[router.Id];
        Assert.Equal(7.0, schedule.Duration);
        Assert.Equal(2.0, schedule.StartTime);
        Assert.Equal(9.0, schedule.FinishTime);
    }

    #endregion

    #region CalculateRouterNodeSchedules - Selected Branch Tests

    [Fact]
    public void CalculateRouterNodeSchedules_WithExecutionSelection_ShouldUseSelectedBranch()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ]);

        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 1.0, 9.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        var routerSelections = new Dictionary<Guid, Guid> { [router.Id] = branchATargetId };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings, routerSelections);

        // Assert - uses selected branch A, not max
        var schedule = result[router.Id];
        Assert.Equal(8.0, schedule.Duration);
        Assert.Equal(1.0, schedule.StartTime);
        Assert.Equal(9.0, schedule.FinishTime);
    }

    [Fact]
    public void CalculateRouterNodeSchedules_SelectedTargetWithNoTiming_ShouldFallBackToOriginal()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, 1.0, [
            CreateBranch("BranchA", branchATargetId)
        ], branchATargetId);

        var allNodes = new List<Node> { router };
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>();

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert - selected target has no timing, falls back to original
        var schedule = result[router.Id];
        Assert.Equal(5.0, schedule.Duration);
        Assert.Equal(1.0, schedule.StartTime);
        Assert.Equal(6.0, schedule.FinishTime);
    }

    #endregion

    #region CalculateRouterNodeSchedules - Argument Validation Tests

    [Fact]
    public void CalculateRouterNodeSchedules_NullAllNodes_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateRouterNodeSchedules(null!,
                new Dictionary<Guid, (double, double, double)>()));
    }

    [Fact]
    public void CalculateRouterNodeSchedules_NullNodeTimings_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.CalculateRouterNodeSchedules(new List<Node>(), null!));
    }

    [Fact]
    public void CalculateRouterNodeSchedules_EmptyNodes_ShouldReturnEmptyDictionary()
    {
        var result = _calculator.CalculateRouterNodeSchedules(
            new List<Node>(),
            new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>());

        Assert.Empty(result);
    }

    #endregion

    #region Selection Priority Tests

    [Fact]
    public void CalculateRouterNodeDuration_ExecutionSelectionOverridesExecutionState()
    {
        // Arrange - both execution selection and execution state exist
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ], selectedBranchTargetNodeId: branchBTargetId); // execution state says B

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Execution-time selection overrides to A
        var routerSelections = new Dictionary<Guid, Guid> { [router.Id] = branchATargetId };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(
            router, new List<Node> { router }, nodeTimings, routerSelections);

        // Assert - execution selection (A = 8) wins over execution state (B = 12)
        Assert.Equal(8.0, result);
    }

    [Fact]
    public void CalculateRouterNodeDuration_ExecutionStateOverridesManualSelection()
    {
        // Arrange
        var branchATargetId = Guid.NewGuid();
        var branchBTargetId = Guid.NewGuid();
        var router = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", branchATargetId),
            CreateBranch("BranchB", branchBTargetId)
        ], selectedBranchTargetNodeId: branchATargetId, manuallySelectedBranch: "BranchB");

        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [branchATargetId] = (8.0, 0.0, 8.0),
            [branchBTargetId] = (12.0, 0.0, 12.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeDuration(router, new List<Node> { router }, nodeTimings);

        // Assert - execution state (A = 8) wins over manual (B = 12)
        Assert.Equal(8.0, result);
    }

    #endregion

    #region Nested Router Duration Tests — cascading underestimation

    [Fact]
    public void CalculateRouterNodeSchedules_NestedRouterInsideBranch_OuterRouterMustReflectInnerRouterDuration()
    {
        // Router1 selects bt1. Router2 (nested inside bt1) selects bt2.
        // bt2 has timing (20.0). With ChildNodeCollector now including RouterNode children,
        // bt1's timing is correctly computed upstream to include the inner router's duration.
        var bt1Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();

        var outerRouter = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("Branch1", bt1Id)
        ], selectedBranchTargetNodeId: bt1Id);

        var innerRouter = CreateRouterNode("Router2", 3.0, branches:
        [
            CreateBranch("InnerBranch", bt2Id)
        ], selectedBranchTargetNodeId: bt2Id);

        var allNodes = new List<Node> { outerRouter, innerRouter };

        // bt1 timing is now correctly computed upstream (includes inner router's duration)
        // bt2 timing is correct (20) — the inner router's branch.
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [bt1Id] = (20.0, 0.0, 20.0),
            [bt2Id] = (20.0, 0.0, 20.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert — outer router must reflect inner router's real duration
        result[innerRouter.Id].Duration.Should().Be(20.0, "Inner router correctly uses bt2 timing");
        result[outerRouter.Id].Duration.Should().BeGreaterThanOrEqualTo(20.0,
            "Router1 must reflect the inner Router2's duration (20) via bt1's correctly computed timing");
    }

    [Fact]
    public void CalculateRouterNodeSchedules_NoSelection_BranchTargetWithNestedRouter_MaxMustReflectTrueDuration()
    {
        // When no branch is selected, max across all branches is used.
        // With ChildNodeCollector now including RouterNode children, bt2's timing is
        // correctly computed upstream to include its nested router's duration (25).
        var bt1Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();

        var outerRouter = CreateRouterNode("Router1", 5.0, branches:
        [
            CreateBranch("BranchA", bt1Id),
            CreateBranch("BranchB", bt2Id)
        ]);

        var allNodes = new List<Node> { outerRouter };

        // bt2 is now correctly computed upstream to include its nested router's duration
        var nodeTimings = new Dictionary<Guid, (double Duration, double StartTime, double FinishTime)>
        {
            [bt1Id] = (10.0, 0.0, 10.0),
            [bt2Id] = (25.0, 0.0, 25.0)
        };

        // Act
        var result = _calculator.CalculateRouterNodeSchedules(allNodes, nodeTimings);

        // Assert — max must be 25 (bt2's true duration)
        result[outerRouter.Id].Duration.Should().Be(25.0,
            "Max of branches must use bt2's true duration (25) including its nested router");
    }

    #endregion

    #region Test Helper Methods

    private static RouterNode CreateRouterNode(
        string name,
        double duration,
        double startTime = 0.0,
        ConditionalBranch[]? branches = null,
        Guid? selectedBranchTargetNodeId = null,
        string? manuallySelectedBranch = null)
    {
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = duration,
                StartTime = startTime,
                FinishTime = startTime + duration,
                Selector = new SimpleVariableSelector
                {
                    Expression = "test_var"
                },
                Branches = branches ?? [],
                SelectedBranchTargetNodeId = selectedBranchTargetNodeId,
                ManuallySelectedBranch = manuallySelectedBranch
            }
        };
    }

    private static ConditionalBranch CreateBranch(string name, Guid? targetNodeId, int priority = 0)
    {
        return new ConditionalBranch
        {
            Name = name,
            Condition = $"{name}_condition",
            Priority = priority,
            TargetNodeId = targetNodeId
        };
    }

    #endregion
}