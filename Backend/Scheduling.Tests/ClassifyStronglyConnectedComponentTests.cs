using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Scheduling.Tests;

public class StronglyConnectedComponentExtensionsTests
{
    [Fact]
    public void ClassifyStronglyConnectedComponent_SingleTask_ReturnsTrivial()
    {
        // Arrange
        var mockTask = new Mock<IPlannedSkillExecution>();
        var tasks = new List<IPlannedSkillExecution> { mockTask.Object };

        var mockScc = new Mock<IStronglyConnectedComponent>();
        mockScc.SetupGet(s => s.SkillExecutions).Returns(tasks);

        // Act
        var result = mockScc.Object.ClassifyStronglyConnectedComponent();

        // Assert
        Assert.Equal(StronglyConnectedComponentKind.Trivial, result.Kind);
        Assert.Same(tasks, result.Tasks);
    }

    [Fact]
    public void ClassifyStronglyConnectedComponent_AdaptiveSsChainWithFfWraparound_ReturnsAdaptiveCycle()
    {
        // Arrange: the benchmark's adaptive ring shape — SS forward chain + FF wraparound.
        var mockAdaptive1 = new Mock<IAdaptivePlannedSkillExecution>();
        var mockAdaptive2 = new Mock<IAdaptivePlannedSkillExecution>();
        var mockAdaptive3 = new Mock<IAdaptivePlannedSkillExecution>();
        var tasks = new List<IPlannedSkillExecution>
        {
            mockAdaptive1.Object, mockAdaptive2.Object, mockAdaptive3.Object
        };

        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.Empty, Source = mockAdaptive1.Object, Target = mockAdaptive2.Object,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.Empty, Source = mockAdaptive2.Object, Target = mockAdaptive3.Object,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.Empty, Source = mockAdaptive3.Object, Target = mockAdaptive1.Object,
                Type = DependencyType.FinishToFinish
            }
        };

        var mockScc = new Mock<IStronglyConnectedComponent>();
        mockScc.SetupGet(s => s.SkillExecutions).Returns(tasks);
        mockScc.SetupGet(s => s.Dependencies).Returns(dependencies);

        // Act
        var result = mockScc.Object.ClassifyStronglyConnectedComponent();

        // Assert
        Assert.Equal(StronglyConnectedComponentKind.AdaptiveCycle, result.Kind);
        Assert.Same(tasks, result.Tasks);
    }

    [Fact]
    public void StronglyConnectedComponentKind_HasOnlyThreeMembers_AfterFixedCycleRemoval()
    {
        // The classifier no longer reports FixedCycle as a kind; that case is
        // handled upstream by ValidateModel as a structural model violation.
        var names = Enum.GetNames<StronglyConnectedComponentKind>();
        Assert.Equal(3, names.Length);
        Assert.Contains(nameof(StronglyConnectedComponentKind.Trivial), names);
        Assert.Contains(nameof(StronglyConnectedComponentKind.AdaptiveCycle), names);
        Assert.Contains(nameof(StronglyConnectedComponentKind.FixedCoupledGroup), names);
    }

    [Fact]
    public void ClassifyStronglyConnectedComponent_MultipleFixedTasks_WithOnlySsOrFfEdges_ReturnsFixedCoupledGroup()
    {
        // Arrange
        var mockTask1 = new Mock<IPlannedSkillExecution>();
        var mockTask2 = new Mock<IPlannedSkillExecution>();
        var tasks = new List<IPlannedSkillExecution> { mockTask1.Object, mockTask2.Object };

        // Only SS/FF edges - no FS cycle, just coupled tasks
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.Empty, Source = mockTask1.Object, Target = mockTask2.Object,
                Type = DependencyType.StartToStart
            },
            new()
            {
                Id = Guid.Empty, Source = mockTask1.Object, Target = mockTask2.Object,
                Type = DependencyType.FinishToFinish
            }
        };

        var mockScc = new Mock<IStronglyConnectedComponent>();
        mockScc.SetupGet(s => s.SkillExecutions).Returns(tasks);
        mockScc.SetupGet(s => s.Dependencies).Returns(dependencies);

        // Act
        var result = mockScc.Object.ClassifyStronglyConnectedComponent();

        // Assert
        Assert.Equal(StronglyConnectedComponentKind.FixedCoupledGroup, result.Kind);
        Assert.Same(tasks, result.Tasks);
    }

    [Fact]
    public void ClassifyStronglyConnectedComponent_MultipleTasksOneAdaptive_ReturnsAdaptiveCycle()
    {
        // Arrange
        var mockFixedTask = new Mock<IPlannedSkillExecution>();
        var mockAdaptiveTask = new Mock<IAdaptivePlannedSkillExecution>(); // Inherits ISkillExecution
        var tasks = new List<IPlannedSkillExecution> { mockFixedTask.Object, mockAdaptiveTask.Object };

        var mockScc = new Mock<IStronglyConnectedComponent>();
        mockScc.SetupGet(s => s.SkillExecutions).Returns(tasks);

        // Act
        var result = mockScc.Object.ClassifyStronglyConnectedComponent();

        // Assert
        Assert.Equal(StronglyConnectedComponentKind.AdaptiveCycle, result.Kind);
        Assert.Same(tasks, result.Tasks);
    }

    [Fact]
    public void ClassifyStronglyConnectedComponent_MultipleAdaptiveTasks_ReturnsAdaptiveCycle()
    {
        // Arrange
        var mockAdaptiveTask1 = new Mock<IAdaptivePlannedSkillExecution>();
        var mockAdaptiveTask2 = new Mock<IAdaptivePlannedSkillExecution>();
        var tasks = new List<IPlannedSkillExecution> { mockAdaptiveTask1.Object, mockAdaptiveTask2.Object };

        var mockScc = new Mock<IStronglyConnectedComponent>();
        mockScc.SetupGet(s => s.SkillExecutions).Returns(tasks);

        // Act
        var result = mockScc.Object.ClassifyStronglyConnectedComponent();

        // Assert
        Assert.Equal(StronglyConnectedComponentKind.AdaptiveCycle, result.Kind);
        Assert.Same(tasks, result.Tasks);
    }

    [Fact]
    public void ClassifyStronglyConnectedComponent_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        IStronglyConnectedComponent? scc = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scc!.ClassifyStronglyConnectedComponent());
    }
}