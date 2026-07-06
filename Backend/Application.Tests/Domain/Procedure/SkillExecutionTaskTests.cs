using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.Application.Tests.Domain.Procedure;

/// <summary>
///     Unit tests for the SkillExecutionTask domain model.
///     Tests verify that the embedded skill object structure works correctly.
/// </summary>
public class SkillExecutionTaskTests
{
    [Fact]
    public void SkillExecutionTask_Should_Use_Skill_Object_References()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        // Create test skill
        var skill = new Skill
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test skill description",
            Properties = []
        };

        // Act
        var skillExecutionTask = new SkillExecutionTask
        {
            Name = "Test Task",
            Description = "Test Description",
            StartTime = 10.0,
            Duration = 5.0,
            Skill = skill,
            AgentId = agentId
        };

        // Assert
        Assert.Equal("Test Task", skillExecutionTask.Name);
        Assert.Equal("Test Description", skillExecutionTask.Description);
        Assert.Equal(10.0, skillExecutionTask.StartTime);
        Assert.Equal(5.0, skillExecutionTask.Duration);
        Assert.Equal(skillId, skillExecutionTask.Skill.Id);
        Assert.Equal(agentId, skillExecutionTask.AgentId);
    }

    [Fact]
    public void SkillExecutionNode_Should_Contain_Skill_Object_Based_Task()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        // Create test skill
        var skill = new Skill
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test skill description",
            Properties = []
        };

        var skillExecutionTask = new SkillExecutionTask
        {
            Name = "Test Task",
            Description = "Test Description",
            StartTime = 10.0,
            Duration = 5.0,
            Skill = skill,
            AgentId = agentId
        };

        // Act
        var skillExecutionNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = nodeId,
            Position = new NodePosition { X = 100, Y = 200 },
            SkillExecutionTask = skillExecutionTask
        };

        // Assert
        Assert.Equal(nodeId, skillExecutionNode.Id);
        Assert.Equal(100, skillExecutionNode.Position.X);
        Assert.Equal(200, skillExecutionNode.Position.Y);
        Assert.Equal(skillId, skillExecutionNode.SkillExecutionTask.Skill.Id);
        Assert.Equal(agentId, skillExecutionNode.SkillExecutionTask.AgentId);
        Assert.Equal("Test Task", skillExecutionNode.SkillExecutionTask.Name);
    }

    [Fact]
    public void SkillExecutionTask_Should_Support_Records_Equality()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        // Create test skill
        var skill = new Skill
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test skill description",
            Properties = []
        };

        var task1 = new SkillExecutionTask
        {
            Name = "Test Task",
            Description = "Test Description",
            StartTime = 10.0,
            Duration = 5.0,
            Skill = skill,
            AgentId = agentId
        };

        var task2 = new SkillExecutionTask
        {
            Name = "Test Task",
            Description = "Test Description",
            StartTime = 10.0,
            Duration = 5.0,
            Skill = skill,
            AgentId = agentId
        };

        // Act & Assert
        Assert.Equal(task1, task2);
        Assert.True(task1 == task2);
        Assert.Equal(task1.GetHashCode(), task2.GetHashCode());
    }
}