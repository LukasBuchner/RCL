using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Dependencies;

/// <INodeHierarchyProcessor>
///     DependencyGraphAnalyzer.
///     </summary>
public sealed class DependencyGraphAnalyzerTests
{
    private readonly DependencyGraphAnalyzer _analyzer;
    private readonly Mock<INodeHierarchyProcessor> _mockHierarchyProcessor;
    private readonly Mock<ILogger<DependencyGraphAnalyzer>> _mockLogger;

    public DependencyGraphAnalyzerTests()
    {
        _mockHierarchyProcessor = new Mock<INodeHierarchyProcessor>();
        _mockLogger = new Mock<ILogger<DependencyGraphAnalyzer>>();
        _analyzer = new DependencyGraphAnalyzer(
            _mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            _mockLogger.Object);
    }

    private static SkillExecutionNode CreateSkill(Guid? id = null, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
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

    private static TaskNode CreateTask(Guid? id = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 1
            }
        };
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHierarchyProcessorIsNull()
    {
        // Act
        var act = () => new DependencyGraphAnalyzer(
            null!,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("hierarchyProcessor");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new DependencyGraphAnalyzer(
            _mockHierarchyProcessor.Object,
            new NodeResolver(NullLogger<NodeResolver>.Instance),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void AnalyzeDependencies_ThrowsArgumentNullException_WhenNodesIsNull()
    {
        // Arrange
        var edges = new List<DependencyEdge>();

        // Act
        var act = () => _analyzer.AnalyzeDependencies(null!, edges);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("nodes");
    }

    [Fact]
    public void AnalyzeDependencies_ThrowsArgumentNullException_WhenEdgesIsNull()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act
        var act = () => _analyzer.AnalyzeDependencies(nodes, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("edges");
    }

    [Fact]
    public void AnalyzeDependencies_ReturnsEmptyGraph_WhenNoNodesOrEdges()
    {
        // Arrange
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Should().NotBeNull();
        result.Prerequisites.Should().BeEmpty();
    }

    [Fact]
    public void AnalyzeDependencies_CreatesSingleSkill_WithNoPrerequisites()
    {
        // Arrange
        var skill1 = CreateSkill();
        var nodes = new List<Node> { skill1 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill1.Id);
        var prereqs = result.Prerequisites[skill1.Id];
        prereqs.SkillId.Should().Be(skill1.Id);
        prereqs.StartPrerequisites.Should().BeEmpty();
        prereqs.FinishPrerequisites.Should().BeEmpty();
        prereqs.HasStartPrerequisites.Should().BeFalse();
        prereqs.IsAdaptive.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeDependencies_CreatesFinishToStartDependency_WithRightLeftHandles()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2 };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill2.Id,
            SourceHandle = "right", // Finish
            TargetHandle = "left" // Start
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill2.Id);
        var prereqs = result.Prerequisites[skill2.Id];
        prereqs.StartPrerequisites.Should().HaveCount(1);
        prereqs.StartPrerequisites[0].DependencySkillId.Should().Be(skill1.Id);
        prereqs.StartPrerequisites[0].RequiredEventType.Should().Be(EventTriggerType.Finish);
        prereqs.FinishPrerequisites.Should().BeEmpty();
        prereqs.IsAdaptive.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeDependencies_CreatesStartToStartDependency_WithLeftLeftHandles()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2 };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill2.Id,
            SourceHandle = "left", // Start
            TargetHandle = "left" // Start
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill2.Id);
        var prereqs = result.Prerequisites[skill2.Id];
        prereqs.StartPrerequisites.Should().HaveCount(1);
        prereqs.StartPrerequisites[0].DependencySkillId.Should().Be(skill1.Id);
        prereqs.StartPrerequisites[0].RequiredEventType.Should().Be(EventTriggerType.Start);
    }

    [Fact]
    public void AnalyzeDependencies_CreatesAdaptiveSkill_WithFinishPrerequisites()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2 };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill2.Id,
            SourceHandle = "right", // Finish
            TargetHandle = "right" // Finish (adaptive!)
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill2.Id);
        var prereqs = result.Prerequisites[skill2.Id];
        prereqs.StartPrerequisites.Should().BeEmpty();
        prereqs.FinishPrerequisites.Should().HaveCount(1);
        prereqs.FinishPrerequisites[0].DependencySkillId.Should().Be(skill1.Id);
        prereqs.FinishPrerequisites[0].RequiredEventType.Should().Be(EventTriggerType.Finish);
        prereqs.IsAdaptive.Should().BeTrue();
        prereqs.HasFinishPrerequisites.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeDependencies_HandlesTaskNodeDependencies_PropagatedToChildSkills()
    {
        // Arrange
        var task1 = CreateTask();
        var skill1 = CreateSkill(parentId: task1.Id);
        var skill2 = CreateSkill(parentId: task1.Id);
        var task2 = CreateTask();
        var skill3 = CreateSkill(parentId: task2.Id);

        var nodes = new List<Node> { task1, skill1, skill2, task2, skill3 };

        // Edge from task1 to task2 should propagate to all child skills
        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = task1.Id,
            TargetId = task2.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { task1, task2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2, skill3 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { task1.Id, new List<Node> { skill1, skill2 }.AsReadOnly() },
                { task2.Id, new List<Node> { skill3 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { task1.Id, new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly() },
                { task2.Id, new List<SkillExecutionNode> { skill3 }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skill1.Id, task1 },
                { skill2.Id, task1 },
                { skill3.Id, task2 }
            }
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill3.Id);
        var prereqs = result.Prerequisites[skill3.Id];

        // skill3 should wait for BOTH skill1 AND skill2 to finish
        prereqs.StartPrerequisites.Should().HaveCount(2);
        prereqs.StartPrerequisites.Should().Contain(p => p.DependencySkillId == skill1.Id);
        prereqs.StartPrerequisites.Should().Contain(p => p.DependencySkillId == skill2.Id);
        prereqs.StartPrerequisites.All(p => p.RequiredEventType == EventTriggerType.Finish).Should().BeTrue();
    }

    [Fact]
    public void AnalyzeDependencies_NestedTaskSource_PropagatesToNestedSkill()
    {
        // Task A -> sub-Task B -> Skill S, with a sibling Skill T and edge A -> T.
        // Single-level source resolution dropped S (T started early); the recursive resolver
        // must give T a finish-prerequisite on the nested S.
        var taskA = CreateTask();
        var taskB = CreateTask();
        taskB.ParentId = taskA.Id;
        var skillS = CreateSkill(parentId: taskB.Id);
        var skillT = CreateSkill();

        var nodes = new List<Node> { taskA, taskB, skillS, skillT };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = taskA.Id,
            TargetId = skillT.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { taskA, taskB }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillS, skillT }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { taskA.Id, new List<Node> { taskB }.AsReadOnly() },
                { taskB.Id, new List<Node> { skillS }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { taskA.Id, Array.Empty<SkillExecutionNode>().AsReadOnly() },
                { taskB.Id, new List<SkillExecutionNode> { skillS }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode> { { skillS.Id, taskB } }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skillT.Id);
        var prereqs = result.Prerequisites[skillT.Id];
        prereqs.StartPrerequisites.Should().ContainSingle()
            .Which.DependencySkillId.Should().Be(skillS.Id);
        prereqs.StartPrerequisites.Single().RequiredEventType.Should().Be(EventTriggerType.Finish);
    }

    [Fact]
    public void AnalyzeDependencies_HandlesMultiplePrerequisites_ForSingleSkill()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var skill3 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2, skill3 };

        // skill3 depends on both skill1 and skill2
        var edge1 = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill3.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };

        var edge2 = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill2.Id,
            TargetId = skill3.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };

        var edges = new List<DependencyEdge> { edge1, edge2 };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2, skill3 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert
        result.Prerequisites.Should().ContainKey(skill3.Id);
        var prereqs = result.Prerequisites[skill3.Id];
        prereqs.StartPrerequisites.Should().HaveCount(2);
        prereqs.StartPrerequisites.Should().Contain(p => p.DependencySkillId == skill1.Id);
        prereqs.StartPrerequisites.Should().Contain(p => p.DependencySkillId == skill2.Id);
    }

    [Fact]
    public void DependencyGraph_GetImmediateStartSkills_ReturnsSkillsWithNoStartPrereqs()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2 };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill2.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);
        var immediateStarts = result.GetImmediateStartSkills().ToList();

        // Assert
        immediateStarts.Should().HaveCount(1);
        immediateStarts.Should().Contain(skill1.Id);
        immediateStarts.Should().NotContain(skill2.Id);
    }

    [Fact]
    public void AnalyzeDependencies_ExternalSkillWithFSToRouter_GetsRouterFinishPrerequisite()
    {
        // Arrange: Router with a branch skill inside, plus an external skill with FS edge from router.
        // The external skill should get a StartPrerequisite on the router's Finish event.
        var routerId = Guid.NewGuid();
        var branchTaskId = Guid.NewGuid();
        var branchSkillId = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();

        var router = new RouterNode
        {
            Id = routerId,
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Quality Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "OK Branch",
                        TargetNodeId = branchTaskId
                    }
                }
            }
        };

        var branchTask = new TaskNode
        {
            Id = branchTaskId,
            ProcedureId = router.ProcedureId,
            ParentId = routerId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Branch Task", StartTime = 0, Duration = 1 }
        };

        var branchSkill = CreateSkill(branchSkillId, branchTaskId);
        branchSkill.SkillExecutionTask.Skill.Name = "Branch Skill";

        var externalSkill = CreateSkill(externalSkillId);
        externalSkill.SkillExecutionTask.Skill.Name = "External Skill";

        var nodes = new List<Node> { router, branchTask, branchSkill, externalSkill };

        // FS edge: Router (right/Finish) → External Skill (left/Start)
        var fsEdge = new DependencyEdge
        {
            ProcedureId = router.ProcedureId,
            Id = Guid.NewGuid(),
            SourceId = routerId,
            TargetId = externalSkillId,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { fsEdge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { branchSkill, externalSkill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { routerId, new List<Node> { branchTask }.AsReadOnly() },
                { branchTaskId, new List<Node> { branchSkill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTaskId, new List<SkillExecutionNode> { branchSkill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { branchSkillId, branchTask }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert: External skill must have a start prerequisite on router Finish
        result.Prerequisites.Should().ContainKey(externalSkillId);
        var externalPrereqs = result.Prerequisites[externalSkillId];
        externalPrereqs.StartPrerequisites.Should().HaveCount(1,
            "external skill should have exactly one start prerequisite (the router's Finish event)");
        externalPrereqs.StartPrerequisites[0].DependencySkillId.Should().Be(routerId,
            "the prerequisite should reference the router node");
        externalPrereqs.StartPrerequisites[0].RequiredEventType.Should().Be(EventTriggerType.Finish,
            "the prerequisite should wait for the router's Finish event");
        externalPrereqs.HasStartPrerequisites.Should().BeTrue(
            "external skill must NOT start immediately — it must wait for the router to complete");
    }

    [Fact]
    public void DependencyGraph_GetAdaptiveSkills_ReturnsSkillsWithFinishPrereqs()
    {
        // Arrange
        var skill1 = CreateSkill();
        var skill2 = CreateSkill();
        var nodes = new List<Node> { skill1, skill2 };

        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skill1.Id,
            TargetId = skill2.Id,
            SourceHandle = "right",
            TargetHandle = "right" // Adaptive
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode>().AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1, skill2 }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>(),
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor
            .Setup(x => x.ProcessHierarchy(nodes))
            .Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);
        var adaptiveSkills = result.GetAdaptiveSkills().ToList();

        // Assert
        adaptiveSkills.Should().HaveCount(1);
        adaptiveSkills.Should().Contain(skill2.Id);
    }

    [Fact]
    public void AnalyzeDependencies_EmptyTaskBetweenSkills_PreservesOrderingThroughTheTask()
    {
        // A -> Task(empty) -> B. The empty task is a leafless firing endpoint: B must gate on the task's
        // Finish and the task must gate on A's Finish, so B cannot start before A. This is the safety case.
        var skillA = CreateSkill();
        var emptyTask = CreateTask();
        var skillB = CreateSkill();

        var nodes = new List<Node> { skillA, emptyTask, skillB };

        var edgeIn = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillA.Id,
            TargetId = emptyTask.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edgeOut = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = emptyTask.Id,
            TargetId = skillB.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edgeIn, edgeOut };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { emptyTask }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillA, skillB }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { emptyTask.Id, Array.Empty<Node>().AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — B gates on the empty task's Finish rather than starting immediately
        result.Prerequisites.Should().ContainKey(skillB.Id);
        result.Prerequisites[skillB.Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == emptyTask.Id && p.RequiredEventType == EventTriggerType.Finish,
            "B must wait for the empty task's Finish");

        // Assert — the empty task is itself a gated firing endpoint waiting on A's Finish
        result.Prerequisites.Should().ContainKey(emptyTask.Id);
        result.Prerequisites[emptyTask.Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == skillA.Id && p.RequiredEventType == EventTriggerType.Finish,
            "the empty task must wait for A's Finish before firing");
    }

    [Fact]
    public void AnalyzeDependencies_NestedEmptyContainer_GatesOnReferencedContainer_NoAncestorWalk()
    {
        // A -> outer -> B, where outer contains an empty inner task (both leafless). The flat wrapper makes
        // the edge-referenced container (outer) the firing endpoint: B gates on outer.Finish and outer gates
        // on A.Finish. The inner task is unreferenced and carries no prerequisite chain.
        var skillA = CreateSkill();
        var outer = CreateTask();
        var inner = CreateTask();
        inner.ParentId = outer.Id;
        var skillB = CreateSkill();

        var nodes = new List<Node> { skillA, outer, inner, skillB };

        var edgeIn = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillA.Id,
            TargetId = outer.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edgeOut = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = outer.Id,
            TargetId = skillB.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edgeIn, edgeOut };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { outer, inner }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillA, skillB }.AsReadOnly(),
            RouterNodes = Array.Empty<RouterNode>().AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { outer.Id, new List<Node> { inner }.AsReadOnly() },
                { inner.Id, Array.Empty<Node>().AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — B gates on the outer container (the edge-referenced firing node), not the inner task
        result.Prerequisites[skillB.Id].StartPrerequisites.Should().ContainSingle(p =>
            p.DependencySkillId == outer.Id && p.RequiredEventType == EventTriggerType.Finish);

        // Assert — the outer container gates on A.Finish
        result.Prerequisites[outer.Id].StartPrerequisites.Should().ContainSingle(p =>
            p.DependencySkillId == skillA.Id && p.RequiredEventType == EventTriggerType.Finish);
    }

    #region Nested Router Tests

    // =====================================================================================
    // These tests probe whether the system can support nested routers:
    //   Router1 → BranchTask1 (ParentId=Router1) → Router2 (ParentId=BranchTask1)
    //                                                 → BranchTask2 (ParentId=Router2)
    //                                                     → Skill (ParentId=BranchTask2)
    // =====================================================================================

    private static RouterNode CreateRouter(Guid? id = null, Guid? parentId = null, Guid? branchTargetId = null)
    {
        var routerId = id ?? Guid.NewGuid();
        return new RouterNode
        {
            Id = routerId,
            ProcedureId = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch", TargetNodeId = branchTargetId, Priority = 0 }
                }
            }
        };
    }

    [Fact]
    public void AnalyzeDependencies_SkillInsideSingleRouter_GetsRouterStartPrerequisite()
    {
        // Baseline: confirm the existing single-router behavior works
        // Router1 → BranchTask → Skill
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;
        var skill = CreateSkill(skillId, branchTask1Id);

        var nodes = new List<Node> { router1, branchTask1, skill };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTask1Id, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, branchTask1 }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — skill should have Router1.Start as prerequisite
        result.Prerequisites.Should().ContainKey(skillId);
        var prereqs = result.Prerequisites[skillId];
        prereqs.StartPrerequisites.Should().HaveCount(1);
        prereqs.StartPrerequisites[0].DependencySkillId.Should().Be(router1Id);
        prereqs.StartPrerequisites[0].RequiredEventType.Should().Be(EventTriggerType.Start);
    }

    [Fact]
    public void AnalyzeDependencies_SkillInsideNestedRouter_GetsInnerRouterStartPrerequisite()
    {
        // Structure: Router1 → BranchTask1 → Router2 → BranchTask2 → Skill
        // FindAncestorRouter walks UP: Skill → BranchTask2 → Router2 (FOUND)
        // So skill should get Router2.Start, NOT Router1.Start
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var skill = CreateSkill(skillId, branchTask2Id);

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2, skill };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() },
                { branchTask2Id, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTask2Id, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, branchTask2 }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — skill gets ONLY Router2.Start (the closest ancestor router)
        result.Prerequisites.Should().ContainKey(skillId);
        var prereqs = result.Prerequisites[skillId];
        prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router2Id && p.RequiredEventType == EventTriggerType.Start,
            "skill should wait for its IMMEDIATE ancestor router (Router2), not the outer one");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouter_DoesNotGetOuterRouterStartPrerequisite()
    {
        // CRITICAL BUG PROBE: Router2 sits inside Router1's branch.
        // Router2 should NOT be a start-signal — it should wait for Router1.Start.
        // But FindAncestorRouter only works for SkillExecutionNodes, not RouterNodes.
        // So Router2 gets NO ancestor router prerequisite → treated as immediate start.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var skill = CreateSkill(skillId, branchTask2Id);

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2, skill };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() },
                { branchTask2Id, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTask2Id, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, branchTask2 }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — Router2 should have Router1.Start as a prerequisite
        // so it doesn't fire before Router1 selects its branch.
        result.Prerequisites.Should().ContainKey(router2Id);
        var router2Prereqs = result.Prerequisites[router2Id];
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "nested Router2 must wait for outer Router1.Start before evaluating its own branches");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouterWithNoEdges_MustHaveAncestorRouterPrerequisite()
    {
        // Router2 inside Router1's branch must wait for Router1.Start before evaluating.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — Router2 must wait for Router1.Start
        result.Prerequisites.Should().ContainKey(router2Id);
        var router2Prereqs = result.Prerequisites[router2Id];
        router2Prereqs.HasStartPrerequisites.Should().BeTrue(
            "nested Router2 must have start prerequisites so it doesn't fire immediately");
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router2 must wait for Router1.Start before evaluating its own branches");
    }

    [Fact]
    public void AnalyzeDependencies_SkillInNestedRouter_OnlyGetsInnerRouterPrereq_NotBothRouters()
    {
        // Probes whether a skill deeply nested under two routers gets prerequisites
        // for BOTH routers or only the closest one.
        // Current behavior: FindAncestorRouter returns first (closest) router only.
        // This means the skill waits for Router2.Start but NOT Router1.Start.
        // If Router2 correctly waits for Router1.Start, this chains properly.
        // If Router2 does NOT wait (the bug above), the skill can fire prematurely.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var skill = CreateSkill(skillId, branchTask2Id);

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2, skill };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() },
                { branchTask2Id, new List<Node> { skill }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTask2Id, new List<SkillExecutionNode> { skill }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillId, branchTask2 }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — skill gets Router2.Start ONLY (closest ancestor)
        var prereqs = result.Prerequisites[skillId];
        prereqs.StartPrerequisites.Should().HaveCount(1,
            "skill should only have one router prerequisite (the closest ancestor)");
        prereqs.StartPrerequisites.Should().NotContain(
            p => p.DependencySkillId == router1Id,
            "skill should NOT directly depend on the outer Router1 — that dependency " +
            "should be transitive through Router2 depending on Router1");
    }

    [Fact]
    public void AnalyzeDependencies_EdgeFromSkillToNestedRouter_WorksCorrectly()
    {
        // Structure: Router1 → BranchTask1 → [Skill1, Router2]
        //            Edge: Skill1 → Router2 (both share ParentId = BranchTask1)
        // Router2 should get Skill1.Finish as prerequisite from the edge.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var skill1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var skill1 = CreateSkill(skill1Id, branchTask1Id);

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var nodes = new List<Node> { router1, branchTask1, skill1, router2, branchTask2 };

        // Edge: Skill1 (Finish) → Router2 (Start)
        var edge = new DependencyEdge
        {
            ProcedureId = router1.ProcedureId,
            Id = Guid.NewGuid(),
            SourceId = skill1Id,
            TargetId = router2Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skill1 }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { skill1, router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { branchTask1Id, new List<SkillExecutionNode> { skill1 }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skill1Id, branchTask1 }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — Router2 should have Skill1.Finish as a start prerequisite (from the edge)
        result.Prerequisites.Should().ContainKey(router2Id);
        var router2Prereqs = result.Prerequisites[router2Id];
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == skill1Id && p.RequiredEventType == EventTriggerType.Finish,
            "Router2 should wait for Skill1 to finish before evaluating (explicit edge dependency)");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouterBothInPrerequisitesMap()
    {
        // Both routers must appear in the prerequisites map for the trigger service to work.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — both routers must be present in the graph
        result.Prerequisites.Should().ContainKey(router1Id, "outer router must be in prerequisites map");
        result.Prerequisites.Should().ContainKey(router2Id, "nested router must be in prerequisites map");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouter_MustNotAppearInImmediateStartSkills()
    {
        // GetImmediateStartSkills() drives what fires at execution start.
        // Only Router1 (top-level) should be immediate. Router2 must wait for Router1.Start.
        var router1Id = Guid.NewGuid();
        var branchTask1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var branchTask2Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: branchTask1Id);
        var branchTask1 = CreateTask(branchTask1Id);
        branchTask1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, branchTask1Id, branchTask2Id);
        var branchTask2 = CreateTask(branchTask2Id);
        branchTask2.ParentId = router2Id;

        var nodes = new List<Node> { router1, branchTask1, router2, branchTask2 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { branchTask1, branchTask2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { branchTask1 }.AsReadOnly() },
                { branchTask1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { branchTask2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);
        var immediateStarts = result.GetImmediateStartSkills().ToList();

        // Assert
        immediateStarts.Should().Contain(router1Id, "Router1 is top-level and should start immediately");
        immediateStarts.Should().NotContain(router2Id,
            "Router2 is nested inside Router1's branch — it must NOT be an immediate start signal");
    }

    [Fact]
    public void AnalyzeDependencies_ThreeLevelNestedRouters_EachWaitsForItsAncestorRouter()
    {
        // 3 levels: Router1 → BranchTask1 → Router2 → BranchTask2 → Router3 → BranchTask3
        // Router2 must wait for Router1.Start, Router3 must wait for Router2.Start.
        var router1Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();
        var router3Id = Guid.NewGuid();
        var bt3Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: bt1Id);
        var bt1 = CreateTask(bt1Id);
        bt1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, bt1Id, bt2Id);
        var bt2 = CreateTask(bt2Id);
        bt2.ParentId = router2Id;

        var router3 = CreateRouter(router3Id, bt2Id, bt3Id);
        var bt3 = CreateTask(bt3Id);
        bt3.ParentId = router3Id;

        var nodes = new List<Node> { router1, bt1, router2, bt2, router3, bt3 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { bt1, bt2, bt3 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2, router3 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { bt1 }.AsReadOnly() },
                { bt1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { bt2 }.AsReadOnly() },
                { bt2Id, new List<Node> { router3 }.AsReadOnly() },
                { router3Id, new List<Node> { bt3 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — each nested router waits for its closest ancestor router
        result.Prerequisites[router2Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router2 must wait for Router1.Start");
        result.Prerequisites[router3Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router2Id && p.RequiredEventType == EventTriggerType.Start,
            "Router3 must wait for Router2.Start");

        var immediateStarts = result.GetImmediateStartSkills().ToList();
        immediateStarts.Should().ContainSingle(id => id == router1Id,
            "only Router1 (top-level) should be an immediate start");
    }

    [Fact]
    public void AnalyzeDependencies_TwoNestedRoutersInSameBranch_BothWaitForOuterRouter()
    {
        // Router1 → BranchTask1 → [Router2, Router3]  (siblings under same branch)
        // Both Router2 and Router3 must wait for Router1.Start.
        var router1Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();
        var router3Id = Guid.NewGuid();
        var bt3Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: bt1Id);
        var bt1 = CreateTask(bt1Id);
        bt1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, bt1Id, bt2Id);
        var bt2 = CreateTask(bt2Id);
        bt2.ParentId = router2Id;

        var router3 = CreateRouter(router3Id, bt1Id, bt3Id);
        var bt3 = CreateTask(bt3Id);
        bt3.ParentId = router3Id;

        var nodes = new List<Node> { router1, bt1, router2, bt2, router3, bt3 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { bt1, bt2, bt3 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2, router3 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { bt1 }.AsReadOnly() },
                { bt1Id, new List<Node> { router2, router3 }.AsReadOnly() },
                { router2Id, new List<Node> { bt2 }.AsReadOnly() },
                { router3Id, new List<Node> { bt3 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — both sibling routers wait for outer Router1.Start
        result.Prerequisites[router2Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router2 must wait for Router1.Start");
        result.Prerequisites[router3Id].StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router3 must wait for Router1.Start");

        var immediateStarts = result.GetImmediateStartSkills().ToList();
        immediateStarts.Should().NotContain(router2Id, "Router2 must not be an immediate start");
        immediateStarts.Should().NotContain(router3Id, "Router3 must not be an immediate start");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouterInNonSelectedBranch_MustWaitForOuterRouter()
    {
        // Router1 has TWO branches: BranchA and BranchB.
        // Router2 is inside BranchB. Router2 must wait for Router1.Start so the trigger service
        // can filter it out when Router1 selects BranchA instead of BranchB.
        var router1Id = Guid.NewGuid();
        var btAId = Guid.NewGuid();
        var btBId = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();
        var skillAId = Guid.NewGuid();

        var router1 = new RouterNode
        {
            Id = router1Id,
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router1",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "BranchA", TargetNodeId = btAId, Priority = 0, Condition = "x == 1" },
                    new() { Name = "BranchB", TargetNodeId = btBId, Priority = 1 }
                }
            }
        };

        var btA = CreateTask(btAId);
        btA.ParentId = router1Id;
        var btB = CreateTask(btBId);
        btB.ParentId = router1Id;

        var skillA = CreateSkill(skillAId, btAId);

        var router2 = CreateRouter(router2Id, btBId, bt2Id);
        var bt2 = CreateTask(bt2Id);
        bt2.ParentId = router2Id;

        var nodes = new List<Node> { router1, btA, btB, skillA, router2, bt2 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { btA, btB, bt2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { skillA }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { btA, btB }.AsReadOnly() },
                { btAId, new List<Node> { skillA }.AsReadOnly() },
                { btBId, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { bt2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>
            {
                { btAId, new List<SkillExecutionNode> { skillA }.AsReadOnly() }
            },
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>
            {
                { skillAId, btA }
            }
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — SkillA (in BranchA) correctly waits for Router1.Start
        var skillAPrereqs = result.Prerequisites[skillAId];
        skillAPrereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "SkillA correctly waits for Router1.Start (existing behavior works for skills)");

        // Assert — Router2 (in BranchB) MUST wait for Router1.Start
        var router2Prereqs = result.Prerequisites[router2Id];
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router2 in BranchB must wait for Router1.Start so it can be filtered out " +
            "when Router1 selects BranchA instead of BranchB");
    }

    [Fact]
    public void AnalyzeDependencies_NestedRouterWithExplicitEdge_MustAlsoGetAncestorRouterPrereq()
    {
        // Router2 inside Router1's branch, with an explicit edge from an external skill to Router2.
        // Router2 must get BOTH the edge-based prerequisite AND Router1.Start as ancestor prerequisite.
        // Without the ancestor prerequisite, Router2 could fire even if Router1 selects a different branch.
        var router1Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var bt2Id = Guid.NewGuid();
        var externalSkillId = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: bt1Id);
        var bt1 = CreateTask(bt1Id);
        bt1.ParentId = router1Id;

        var router2 = CreateRouter(router2Id, bt1Id, bt2Id);
        var bt2 = CreateTask(bt2Id);
        bt2.ParentId = router2Id;

        var externalSkill = CreateSkill(externalSkillId);

        var nodes = new List<Node> { externalSkill, router1, bt1, router2, bt2 };

        // Edge: ExternalSkill (Finish) → Router2 (Start)
        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = externalSkillId,
            TargetId = router2Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };
        var edges = new List<DependencyEdge> { edge };

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { bt1, bt2 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode> { externalSkill }.AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1, router2 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { bt1 }.AsReadOnly() },
                { bt1Id, new List<Node> { router2 }.AsReadOnly() },
                { router2Id, new List<Node> { bt2 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — Router2 must have BOTH the explicit edge prerequisite AND Router1.Start
        var router2Prereqs = result.Prerequisites[router2Id];
        router2Prereqs.StartPrerequisites.Should().HaveCount(2,
            "Router2 should have 2 prerequisites: explicit edge + ancestor router");
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == externalSkillId && p.RequiredEventType == EventTriggerType.Finish,
            "Router2 must get the edge-based prerequisite from ExternalSkill.Finish");
        router2Prereqs.StartPrerequisites.Should().ContainSingle(
            p => p.DependencySkillId == router1Id && p.RequiredEventType == EventTriggerType.Start,
            "Router2 must also get Router1.Start as ancestor prerequisite to prevent " +
            "firing when Router1 selects a different branch");
    }

    [Fact]
    public void AnalyzeDependencies_TopLevelRouterWithNoParent_HasNoAncestorPrerequisite()
    {
        // Sanity check: a top-level router (no parent) should correctly have no
        // ancestor-router prerequisite. This must keep passing after the fix.
        var router1Id = Guid.NewGuid();
        var bt1Id = Guid.NewGuid();

        var router1 = CreateRouter(router1Id, branchTargetId: bt1Id);
        var bt1 = CreateTask(bt1Id);
        bt1.ParentId = router1Id;

        var nodes = new List<Node> { router1, bt1 };
        var edges = new List<DependencyEdge>();

        var hierarchy = new NodeHierarchyInfo
        {
            TaskNodes = new List<TaskNode> { bt1 }.AsReadOnly(),
            SkillExecutionNodes = new List<SkillExecutionNode>().AsReadOnly(),
            RouterNodes = new List<RouterNode> { router1 }.AsReadOnly(),
            ParentToChildrenMapping = new Dictionary<Guid, IReadOnlyList<Node>>
            {
                { router1Id, new List<Node> { bt1 }.AsReadOnly() }
            },
            TaskToSkillMapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>(),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };

        _mockHierarchyProcessor.Setup(x => x.ProcessHierarchy(nodes)).Returns(hierarchy);

        // Act
        var result = _analyzer.AnalyzeDependencies(nodes, edges);

        // Assert — top-level router should be an immediate start with no prerequisites
        result.Prerequisites[router1Id].StartPrerequisites.Should().BeEmpty(
            "top-level router has no parent and no incoming edges — it starts immediately");
        result.GetImmediateStartSkills().Should().Contain(router1Id);
    }

    #endregion
}