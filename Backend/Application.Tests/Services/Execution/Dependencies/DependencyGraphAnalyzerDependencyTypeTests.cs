using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Dependencies;

/// <summary>
///     Verifies the handle-pair-to-DependencyType mapping in DependencyGraphAnalyzer,
///     and that DependencyType is correctly propagated through TaskNode expansion.
/// </summary>
public sealed class DependencyGraphAnalyzerDependencyTypeTests
{
    private readonly DependencyGraphAnalyzer _analyzer;
    private readonly Mock<INodeHierarchyProcessor> _mockHierarchyProcessor;

    public DependencyGraphAnalyzerDependencyTypeTests()
    {
        _mockHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        _analyzer = new DependencyGraphAnalyzer(
            _mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            new Mock<ILogger<DependencyGraphAnalyzer>>().Object);
    }

    [Fact]
    public void FsEdge_RightToLeft_DependencyTypeIsFinishToStart()
    {
        var result = DependencyGraphAnalyzer.GetDependencyType("right", "left");

        Assert.Equal(DependencyType.FinishToStart, result);
    }

    [Fact]
    public void SsEdge_LeftToLeft_DependencyTypeIsStartToStart()
    {
        var result = DependencyGraphAnalyzer.GetDependencyType("left", "left");

        Assert.Equal(DependencyType.StartToStart, result);
    }

    [Fact]
    public void SfEdge_LeftToRight_DependencyTypeIsStartToFinish()
    {
        var result = DependencyGraphAnalyzer.GetDependencyType("left", "right");

        Assert.Equal(DependencyType.StartToFinish, result);
    }

    [Fact]
    public void FfEdge_RightToRight_DependencyTypeIsFinishToFinish()
    {
        var result = DependencyGraphAnalyzer.GetDependencyType("right", "right");

        Assert.Equal(DependencyType.FinishToFinish, result);
    }

    [Fact]
    public void NullHandles_DefaultToFinish_DependencyTypeIsFinishToFinish()
    {
        var result = DependencyGraphAnalyzer.GetDependencyType(null, null);

        Assert.Equal(DependencyType.FinishToFinish, result);
    }

    /// <summary>
    ///     An SS edge targeting a TaskNode should propagate StartToStart DependencyType
    ///     to all child skills of that TaskNode. This verifies that
    ///     <see cref="DependencyGraphAnalyzer.AnalyzeDependencies"/> preserves the
    ///     DependencyType when expanding TaskNode targets into individual child skill prerequisites.
    /// </summary>
    [Fact]
    public void SsEdgeTargetingTaskNode_PropagatesStartToStartToChildSkills()
    {
        // Arrange
        var skillAId = Guid.NewGuid();
        var taskTId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillCId = Guid.NewGuid();

        var skillA = CreateSkill(skillAId);
        var taskT = CreateTask(taskTId);
        var skillB = CreateSkill(skillBId, taskTId);
        var skillC = CreateSkill(skillCId, taskTId);

        var nodes = new List<Node> { skillA, taskT, skillB, skillC };

        // SS edge: A → T (left → left)
        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillAId,
            TargetId = taskTId,
            SourceHandle = "left",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { taskT }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillA, skillB, skillC }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { taskTId, new List<Node> { skillB, skillC }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { taskTId, new List<SkillExecutionNode> { skillB, skillC }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillBId, taskT },
                { skillCId, taskT }
            }
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — B and C each get an SS start prerequisite on A
        var bPrereqs = result.Prerequisites[skillBId];
        Assert.Single(bPrereqs.StartPrerequisites);
        Assert.Equal(skillAId, bPrereqs.StartPrerequisites[0].DependencySkillId);
        Assert.Equal(EventTriggerType.Start, bPrereqs.StartPrerequisites[0].RequiredEventType);
        Assert.Equal(DependencyType.StartToStart, bPrereqs.StartPrerequisites[0].DependencyType);

        var cPrereqs = result.Prerequisites[skillCId];
        Assert.Single(cPrereqs.StartPrerequisites);
        Assert.Equal(skillAId, cPrereqs.StartPrerequisites[0].DependencySkillId);
        Assert.Equal(EventTriggerType.Start, cPrereqs.StartPrerequisites[0].RequiredEventType);
        Assert.Equal(DependencyType.StartToStart, cPrereqs.StartPrerequisites[0].DependencyType);
    }

    #region Helper Methods

    private static SkillExecutionNode CreateSkill(Guid id, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                StartTime = 0,
                Duration = 1,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test",
                    Description = "Test skill",
                    Properties = new List<TypedProperty>()
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static TaskNode CreateTask(Guid id)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 1
            }
        };
    }

    #endregion
}