using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Tests.TestUtilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Processing.Mapping;

/// <summary>
///     Unit tests for NodeRelationshipMapper service.
///     Uses TestLogger instead of Moq to avoid incompatibility with source-generated logging.
/// </summary>
public class NodeRelationshipMapperTests
{
    private readonly NodeRelationshipMapper _mapper;
    private readonly TestLogger<NodeRelationshipMapper> _testLogger;

    public NodeRelationshipMapperTests()
    {
        _testLogger = new TestLogger<NodeRelationshipMapper>();
        _mapper = new NodeRelationshipMapper(_testLogger);
    }

    [Fact]
    public void BuildTaskToSkillMapping_WithValidHierarchy_ReturnsMappingCorrectly()
    {
        // Arrange
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();
        var skillId3 = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(taskId1, "Task1"),
            CreateTaskNode(taskId2, "Task2")
        }.AsReadOnly();

        var skillNodes = new List<SkillExecutionNode>
        {
            CreateSkillExecutionNode(skillId1, "Skill1", taskId1),
            CreateSkillExecutionNode(skillId2, "Skill2", taskId1),
            CreateSkillExecutionNode(skillId3, "Skill3", taskId2)
        }.AsReadOnly();

        // Act
        var result = _mapper.BuildTaskToSkillMapping(taskNodes, skillNodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[taskId1].Count);
        Assert.Equal(1, result[taskId2].Count);
        Assert.Contains(result[taskId1], s => s.Id == skillId1);
        Assert.Contains(result[taskId1], s => s.Id == skillId2);
        Assert.Contains(result[taskId2], s => s.Id == skillId3);
    }

    [Fact]
    public void BuildTaskToSkillMapping_WithOrphanedSkills_LogsWarning()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(taskId, "Task1")
        }.AsReadOnly();

        var skillNodes = new List<SkillExecutionNode>
        {
            CreateSkillExecutionNode(skillId1, "Skill1", taskId),
            CreateSkillExecutionNode(skillId2, "Skill2", null) // orphaned
        }.AsReadOnly();

        // Act
        var result = _mapper.BuildTaskToSkillMapping(taskNodes, skillNodes);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Equal(1, result[taskId].Count);

        // Verify debug log for orphaned nodes was logged
        var orphanedEntries = _testLogger.GetEntriesContaining("ORPHANED_DETECTED").ToList();
        Assert.Single(orphanedEntries);
        Assert.Contains("ORPHANED_DETECTED", orphanedEntries[0].Message);
    }

    [Fact]
    public void BuildSkillToTaskMapping_WithValidHierarchy_ReturnsMappingCorrectly()
    {
        // Arrange
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(taskId1, "Task1"),
            CreateTaskNode(taskId2, "Task2")
        }.AsReadOnly();

        var skillNodes = new List<SkillExecutionNode>
        {
            CreateSkillExecutionNode(skillId1, "Skill1", taskId1),
            CreateSkillExecutionNode(skillId2, "Skill2", taskId2)
        }.AsReadOnly();

        // Act
        var result = _mapper.BuildSkillToTaskMapping(taskNodes, skillNodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(taskId1, result[skillId1].Id);
        Assert.Equal(taskId2, result[skillId2].Id);
    }

    [Fact]
    public void BuildSkillToTaskMapping_WithInvalidParentReferences_LogsWarning()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var invalidTaskId = Guid.NewGuid();
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();

        var taskNodes = new List<TaskNode>
        {
            CreateTaskNode(taskId, "Task1")
        }.AsReadOnly();

        var skillNodes = new List<SkillExecutionNode>
        {
            CreateSkillExecutionNode(skillId1, "Skill1", taskId),
            CreateSkillExecutionNode(skillId2, "Skill2", invalidTaskId) // invalid parent
        }.AsReadOnly();

        // Act
        var result = _mapper.BuildSkillToTaskMapping(taskNodes, skillNodes);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Equal(taskId, result[skillId1].Id);

        // Verify warning was logged
        var warningEntries = _testLogger.GetEntriesByLevel(LogLevel.Warning).ToList();
        Assert.Single(warningEntries);
        Assert.Contains("INVALID_REFERENCES", warningEntries[0].Message);
    }

    [Fact]
    public void BuildParentToChildrenMapping_WithMixedHierarchy_ReturnsCorrectMapping()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId1 = Guid.NewGuid();
        var childId2 = Guid.NewGuid();
        var rootId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateTaskNode(parentId, "Parent"),
            CreateTaskNode(childId1, "Child1", parentId),
            CreateTaskNode(childId2, "Child2", parentId),
            CreateTaskNode(rootId, "Root") // no parent
        }.AsReadOnly();

        // Act
        var result = _mapper.BuildParentToChildrenMapping(nodes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[parentId].Count);
        Assert.Equal(2, result[Guid.Empty].Count); // root nodes
        Assert.Contains(result[parentId], n => n.Id == childId1);
        Assert.Contains(result[parentId], n => n.Id == childId2);
        Assert.Contains(result[Guid.Empty], n => n.Id == parentId);
        Assert.Contains(result[Guid.Empty], n => n.Id == rootId);
    }

    [Fact]
    public void BuildParentToChildrenMapping_WithEmptyList_ReturnsEmptyMapping()
    {
        // Arrange
        var nodes = new List<Node>().AsReadOnly();

        // Act
        var result = _mapper.BuildParentToChildrenMapping(nodes);

        // Assert
        Assert.Empty(result);
    }

    private static TaskNode CreateTaskNode(Guid id, string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0,
                IsExecuting = false,
                Progress = 0.0
            }
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(Guid id, string skillName, Guid? parentId)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Description = "Test skill",
                    Properties = []
                }
            }
        };
    }
}