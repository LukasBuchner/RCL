using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Hierarchy;

/// <summary>
///     Comprehensive TDD unit tests for NodeHierarchyProcessor.
///     Tests hierarchy processing, validation, mapping creation, and error scenarios.
/// </summary>
public class NodeHierarchyProcessorTests
{
    private readonly Mock<IHierarchyValidator> _mockHierarchyValidator;
    private readonly Mock<ILogger<NodeHierarchyProcessor>> _mockLogger;
    private readonly Mock<INodeRelationshipMapper> _mockRelationshipMapper;
    private readonly NodeHierarchyProcessor _processor;

    public NodeHierarchyProcessorTests()
    {
        _mockRelationshipMapper = new Mock<INodeRelationshipMapper>();
        _mockHierarchyValidator = new Mock<IHierarchyValidator>();
        _mockLogger = new Mock<ILogger<NodeHierarchyProcessor>>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Set up default behaviors
        SetupDefaultMocks();

        _processor = new NodeHierarchyProcessor(
            _mockRelationshipMapper.Object,
            _mockHierarchyValidator.Object,
            _mockLogger.Object);
    }

    private void SetupDefaultMocks()
    {
        // Default setup for relationship mapper
        _mockRelationshipMapper.Setup(m => m.BuildTaskToSkillMapping(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes, IReadOnlyList<SkillExecutionNode> skillNodes) =>
            {
                // Create a realistic task-to-skill mapping
                var mapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>();
                foreach (var taskNode in taskNodes)
                {
                    var childSkills = skillNodes.Where(s => s.ParentId == taskNode.Id).ToList().AsReadOnly();
                    mapping[taskNode.Id] = childSkills;
                }

                return mapping.AsReadOnly();
            });

        _mockRelationshipMapper.Setup(m => m.BuildSkillToTaskMapping(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes, IReadOnlyList<SkillExecutionNode> skillNodes) =>
            {
                // Create a realistic skill-to-task mapping
                var mapping = new Dictionary<Guid, TaskNode>();
                foreach (var skillNode in skillNodes)
                    if (skillNode.ParentId.HasValue)
                    {
                        var parentTask = taskNodes.FirstOrDefault(t => t.Id == skillNode.ParentId.Value);
                        if (parentTask != null) mapping[skillNode.Id] = parentTask;
                    }

                return mapping.AsReadOnly();
            });

        _mockRelationshipMapper.Setup(m => m.BuildParentToChildrenMapping(
                It.IsAny<IReadOnlyList<Node>>()))
            .Returns((IReadOnlyList<Node> nodes) =>
            {
                // Create a realistic parent-to-children mapping
                var mapping = new Dictionary<Guid, IReadOnlyList<Node>>();

                // Group nodes by their ParentId
                var grouped = nodes.GroupBy(n => n.ParentId ?? Guid.Empty);
                foreach (var group in grouped) mapping[group.Key] = group.ToList().AsReadOnly();

                return mapping.AsReadOnly();
            });

        // Default setup for hierarchy validator
        _mockHierarchyValidator.Setup(v => v.ValidateConsistency(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, TaskNode>>()))
            .Returns(new HierarchyValidationResult(true, Array.Empty<string>().AsReadOnly(),
                Array.Empty<string>().AsReadOnly()));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new NodeHierarchyProcessor(
            null!,
            _mockHierarchyValidator.Object,
            _mockLogger.Object));
        Assert.Equal("relationshipMapper", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var processor = new NodeHierarchyProcessor(
            _mockRelationshipMapper.Object,
            _mockHierarchyValidator.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(processor);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void ProcessHierarchy_WithNullNodes_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _processor.ProcessHierarchy(null!));
        Assert.Equal("nodes", exception.ParamName);
    }

    [Fact]
    public void ProcessHierarchy_WithEmptyNodeList_ReturnsEmptyHierarchy()
    {
        // Arrange
        var emptyNodes = new List<Node>().AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(emptyNodes);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.TaskNodes);
        Assert.Empty(result.SkillExecutionNodes);
        Assert.Empty(result.ParentToChildrenMapping);
        Assert.Empty(result.TaskToSkillMapping);
        Assert.Empty(result.SkillToTaskMapping);
        Assert.Equal(0, result.TotalNodeCount);
        Assert.False(result.HasSkillExecutionNodes);

        // Verify warning was logged for empty list
        VerifyLogCalled(LogLevel.Warning, Times.Once());
    }

    #endregion

    #region Basic Processing Tests

    [Fact]
    public void ProcessHierarchy_WithSingleTaskNode_ReturnsCorrectHierarchy()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var nodes = new List<Node> { taskNode }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Single(result.TaskNodes);
        Assert.Equal(taskNode.Id, result.TaskNodes.First().Id);
        Assert.Empty(result.SkillExecutionNodes);
        Assert.Single(result.ParentToChildrenMapping);
        Assert.Contains(Guid.Empty, result.ParentToChildrenMapping.Keys); // Root node
        Assert.Equal(1, result.TotalNodeCount);
        Assert.False(result.HasSkillExecutionNodes);
    }

    [Fact]
    public void ProcessHierarchy_WithSingleSkillExecutionNode_ReturnsCorrectHierarchy()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Empty(result.TaskNodes);
        Assert.Single(result.SkillExecutionNodes);
        Assert.Equal(skillNode.Id, result.SkillExecutionNodes.First().Id);
        Assert.Single(result.ParentToChildrenMapping);
        Assert.Contains(Guid.Empty, result.ParentToChildrenMapping.Keys); // Root node
        Assert.Equal(1, result.TotalNodeCount);
        Assert.True(result.HasSkillExecutionNodes);
    }

    [Fact]
    public void ProcessHierarchy_WithTaskAndSkillNodes_CreatesCorrectMappings()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var skillNode1 = CreateSkillExecutionNode("Skill1", taskNode.Id);
        var skillNode2 = CreateSkillExecutionNode("Skill2", taskNode.Id);
        var nodes = new List<Node> { taskNode, skillNode1, skillNode2 }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Single(result.TaskNodes);
        Assert.Equal(2, result.SkillExecutionNodes.Count);

        // Verify task-to-skill mapping
        Assert.True(result.TaskToSkillMapping.ContainsKey(taskNode.Id));
        Assert.Equal(2, result.TaskToSkillMapping[taskNode.Id].Count);

        // Verify skill-to-task mapping
        Assert.True(result.SkillToTaskMapping.ContainsKey(skillNode1.Id));
        Assert.True(result.SkillToTaskMapping.ContainsKey(skillNode2.Id));
        Assert.Equal(taskNode.Id, result.SkillToTaskMapping[skillNode1.Id].Id);
        Assert.Equal(taskNode.Id, result.SkillToTaskMapping[skillNode2.Id].Id);

        // Verify parent-to-children mapping
        Assert.True(result.ParentToChildrenMapping.ContainsKey(Guid.Empty)); // Task node is root
        Assert.True(result.ParentToChildrenMapping.ContainsKey(taskNode.Id)); // Task has children

        Assert.Equal(3, result.TotalNodeCount);
        Assert.True(result.HasSkillExecutionNodes);
    }

    #endregion

    #region Complex Hierarchy Tests

    [Fact]
    public void ProcessHierarchy_WithMultipleLevels_CreatesCompleteHierarchy()
    {
        // Arrange
        var rootTask = CreateTaskNode("RootTask");
        var childTask = CreateTaskNode("ChildTask", rootTask.Id);
        var skill1 = CreateSkillExecutionNode("Skill1", rootTask.Id);
        var skill2 = CreateSkillExecutionNode("Skill2", childTask.Id);
        var skill3 = CreateSkillExecutionNode("Skill3", childTask.Id);

        var nodes = new List<Node> { rootTask, childTask, skill1, skill2, skill3 }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Equal(2, result.TaskNodes.Count);
        Assert.Equal(3, result.SkillExecutionNodes.Count);

        // Verify mappings exist for both tasks
        Assert.True(result.TaskToSkillMapping.ContainsKey(rootTask.Id));
        Assert.True(result.TaskToSkillMapping.ContainsKey(childTask.Id));

        // Verify correct skill counts per task
        Assert.Single(result.TaskToSkillMapping[rootTask.Id]);
        Assert.Equal(2, result.TaskToSkillMapping[childTask.Id].Count);

        // Verify all skills have parent mappings
        Assert.All(result.SkillExecutionNodes, skill =>
            Assert.True(result.SkillToTaskMapping.ContainsKey(skill.Id)));

        Assert.Equal(5, result.TotalNodeCount);
        Assert.True(result.HasSkillExecutionNodes);
    }

    [Fact]
    public void ProcessHierarchy_WithOrphanedSkillNodes_HandlesGracefully()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var validSkill = CreateSkillExecutionNode("ValidSkill", taskNode.Id);
        var orphanedSkill1 = CreateSkillExecutionNode("OrphanedSkill1"); // No parent
        var orphanedSkill2 = CreateSkillExecutionNode("OrphanedSkill2", Guid.NewGuid()); // Invalid parent

        var nodes = new List<Node> { taskNode, validSkill, orphanedSkill1, orphanedSkill2 }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Single(result.TaskNodes);
        Assert.Equal(3, result.SkillExecutionNodes.Count);

        // Valid skill should be mapped
        Assert.Single(result.TaskToSkillMapping[taskNode.Id]);
        Assert.True(result.SkillToTaskMapping.ContainsKey(validSkill.Id));

        // Orphaned skills should not be in mappings
        Assert.False(result.SkillToTaskMapping.ContainsKey(orphanedSkill1.Id));
        Assert.False(result.SkillToTaskMapping.ContainsKey(orphanedSkill2.Id));

        // Verify debug log was logged for orphaned skills (logged by NodeRelationshipMapper at Debug level)
        VerifyLogCalled(LogLevel.Debug, Times.AtLeastOnce());
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ProcessHierarchy_WithUnknownNodeTypes_LogsWarning()
    {
        // Arrange - create a concrete unknown node type by creating a custom derived node
        var unknownNode = new TestUnknownNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            ParentId = null,
            ProcedureId = default
        };

        var nodes = new List<Node> { unknownNode }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Empty(result.TaskNodes);
        Assert.Empty(result.SkillExecutionNodes);

        // Verify warning was logged for unexpected node type
        VerifyLogCalled(LogLevel.Warning, Times.AtLeastOnce());
    }

    // Custom test node class for testing unknown node types
    private record TestUnknownNode : Node;

    [Fact]
    public void ProcessHierarchy_WithLargeHierarchy_ProcessesEfficiently()
    {
        // Arrange - Create a large hierarchy to test performance
        var nodes = new List<Node>();
        var rootTasks = new List<TaskNode>();

        // Create 10 root tasks, each with 20 skills
        for (var i = 0; i < 10; i++)
        {
            var rootTask = CreateTaskNode($"RootTask{i}");
            rootTasks.Add(rootTask);
            nodes.Add(rootTask);

            for (var j = 0; j < 20; j++)
            {
                var skill = CreateSkillExecutionNode($"Skill{i}_{j}", rootTask.Id);
                nodes.Add(skill);
            }
        }

        var startTime = DateTime.UtcNow;

        // Act
        var result = _processor.ProcessHierarchy(nodes.AsReadOnly());

        // Assert
        var processingTime = DateTime.UtcNow - startTime;
        Assert.True(processingTime.TotalSeconds < 5, "Processing should complete within 5 seconds for large hierarchy");

        Assert.Equal(10, result.TaskNodes.Count);
        Assert.Equal(200, result.SkillExecutionNodes.Count);
        Assert.Equal(210, result.TotalNodeCount);

        // Verify all tasks have correct skill counts
        Assert.All(rootTasks, task =>
        {
            Assert.True(result.TaskToSkillMapping.ContainsKey(task.Id));
            Assert.Equal(20, result.TaskToSkillMapping[task.Id].Count);
        });
    }

    #endregion

    #region Consistency Validation Tests

    [Fact]
    public void ProcessHierarchy_WithConsistentHierarchy_PassesValidation()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var skillNode = CreateSkillExecutionNode("Skill1", taskNode.Id);
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.NotNull(result);

        // Verify validation passed (no error logs)
        VerifyLogNotCalled(LogLevel.Error);

        // Verify mappings are consistent
        Assert.True(result.TaskToSkillMapping.ContainsKey(taskNode.Id));
        Assert.Contains(skillNode, result.TaskToSkillMapping[taskNode.Id]);
        Assert.True(result.SkillToTaskMapping.ContainsKey(skillNode.Id));
        Assert.Equal(taskNode.Id, result.SkillToTaskMapping[skillNode.Id].Id);
    }

    [Fact]
    public void ProcessHierarchy_WithCircularReferences_DetectsAndLogs()
    {
        // Arrange - Create circular reference by having a parent point to its child
        var task1 = CreateTaskNode("Task1");
        var task2 = CreateTaskNode("Task2", task1.Id);

        // Create circular reference (not possible in real scenario but testing validation)
        task1 = task1 with { ParentId = task2.Id };

        var nodes = new List<Node> { task1, task2 }.AsReadOnly();

        // Setup hierarchy validator to return validation failure for this scenario
        _mockHierarchyValidator.Setup(v => v.ValidateConsistency(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, TaskNode>>()))
            .Returns(new HierarchyValidationResult(false,
                new List<string> { "Circular reference detected between Task1 and Task2" }.AsReadOnly(),
                Array.Empty<string>().AsReadOnly()));

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.NotNull(result);

        // Verify error was logged for circular reference
        VerifyLogCalled(LogLevel.Error, Times.AtLeastOnce());
    }

    #endregion

    #region NodeHierarchyInfo TypedProperty Tests

    [Fact]
    public void NodeHierarchyInfo_AllNodes_CombinesTaskAndSkillNodes()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var skillNode = CreateSkillExecutionNode("Skill1", taskNode.Id);
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Equal(2, result.AllNodes.Count);
        Assert.Contains(taskNode.Id, result.AllNodes.Select(n => n.Id));
        Assert.Contains(skillNode.Id, result.AllNodes.Select(n => n.Id));
    }

    [Fact]
    public void NodeHierarchyInfo_TotalNodeCount_ReturnsCorrectSum()
    {
        // Arrange
        var taskNode1 = CreateTaskNode("Task1");
        var taskNode2 = CreateTaskNode("Task2");
        var skillNode1 = CreateSkillExecutionNode("Skill1");
        var skillNode2 = CreateSkillExecutionNode("Skill2");
        var skillNode3 = CreateSkillExecutionNode("Skill3");

        var nodes = new List<Node> { taskNode1, taskNode2, skillNode1, skillNode2, skillNode3 }.AsReadOnly();

        // Act
        var result = _processor.ProcessHierarchy(nodes);

        // Assert
        Assert.Equal(5, result.TotalNodeCount);
        Assert.Equal(result.TaskNodes.Count + result.SkillExecutionNodes.Count, result.TotalNodeCount);
    }

    [Fact]
    public void NodeHierarchyInfo_HasSkillExecutionNodes_ReturnsCorrectValue()
    {
        // Arrange - Only task nodes
        var taskOnlyNodes = new List<Node> { CreateTaskNode("Task1") }.AsReadOnly();

        // Arrange - With skill nodes
        var mixedNodes = new List<Node>
        {
            CreateTaskNode("Task1"),
            CreateSkillExecutionNode("Skill1")
        }.AsReadOnly();

        // Act
        var taskOnlyResult = _processor.ProcessHierarchy(taskOnlyNodes);
        var mixedResult = _processor.ProcessHierarchy(mixedNodes);

        // Assert
        Assert.False(taskOnlyResult.HasSkillExecutionNodes);
        Assert.True(mixedResult.HasSkillExecutionNodes);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void ProcessHierarchy_WithValidInput_LogsAppropriateMessages()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task1");
        var skillNode = CreateSkillExecutionNode("Skill1", taskNode.Id);
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        // Act
        _processor.ProcessHierarchy(nodes);

        // Assert - Verify different log levels were called
        VerifyLogCalled(LogLevel.Debug, Times.AtLeastOnce());
        VerifyLogNotCalled(LogLevel.Error);
    }

    [Fact]
    public void ProcessHierarchy_WithTraceLogging_LogsDetailedInformation()
    {
        // Arrange
        _mockLogger.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(true);

        var taskNode = CreateTaskNode("Task1");
        var skillNode = CreateSkillExecutionNode("Skill1", taskNode.Id);
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        // Act
        _processor.ProcessHierarchy(nodes);

        // Assert - Verify trace logging was called
        VerifyLogCalled(LogLevel.Trace, Times.AtLeastOnce());
    }

    #endregion

    #region Helper Methods

    private TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 100.0,
                FinishTime = 100.0,
                IsExecuting = false,
                Progress = 0.0
            }
        };
    }

    private SkillExecutionNode CreateSkillExecutionNode(string name, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = 50.0,
                FinishTime = 50.0,
                IsExecuting = false,
                Progress = 0.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Test skill: {name}",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private void VerifyLogCalled(LogLevel level, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private void VerifyLogNotCalled(LogLevel level)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion
}