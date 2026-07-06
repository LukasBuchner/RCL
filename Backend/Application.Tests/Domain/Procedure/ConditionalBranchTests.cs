using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Domain.Procedure;

/// <summary>
///     Tests for the ConditionalBranch record.
/// </summary>
public class ConditionalBranchTests
{
    [Fact]
    public void ConditionalBranch_ShouldCreateWithRequiredFields()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();

        // Act
        var branch = new ConditionalBranch
        {
            Name = "OK Branch",
            TargetNodeId = targetNodeId
        };

        // Assert
        branch.Name.Should().Be("OK Branch");
        branch.TargetNodeId.Should().Be(targetNodeId);
        branch.Condition.Should().BeNull();
        branch.Priority.Should().Be(0);
        branch.TargetNode.Should().BeNull();
    }

    [Fact]
    public void ConditionalBranch_ShouldCreateWithCondition()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();

        // Act
        var branch = new ConditionalBranch
        {
            Name = "OK Branch",
            Condition = "quality_result == 'OK'",
            TargetNodeId = targetNodeId
        };

        // Assert
        branch.Condition.Should().Be("quality_result == 'OK'");
    }

    [Fact]
    public void ConditionalBranch_ShouldCreateWithNullCondition()
    {
        // Arrange & Act
        var branch = new ConditionalBranch
        {
            Name = "Default Branch",
            Condition = null,
            Priority = 999,
            TargetNodeId = Guid.NewGuid()
        };

        // Assert
        branch.Condition.Should().BeNull();
        branch.Name.Should().Be("Default Branch");
    }

    [Fact]
    public void ConditionalBranch_ShouldSetPriority()
    {
        // Arrange & Act
        var branch = new ConditionalBranch
        {
            Name = "High Priority",
            Priority = 1,
            TargetNodeId = Guid.NewGuid()
        };

        // Assert
        branch.Priority.Should().Be(1);
    }

    [Fact]
    public void ConditionalBranch_TargetNode_ShouldBeNavigationProperty()
    {
        // Arrange
        var targetNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = "Target Task",
                Description = "",
                StartTime = 0,
                Duration = 10
            },
            ProcedureId = default
        };

        // Act
        var branch = new ConditionalBranch
        {
            Name = "Test Branch",
            TargetNodeId = targetNode.Id,
            TargetNode = targetNode
        };

        // Assert
        branch.TargetNode.Should().NotBeNull();
        branch.TargetNode!.Id.Should().Be(targetNode.Id);
    }

    [Fact]
    public void IsDefaultBranch_ShouldReturnTrue_WhenConditionIsNull()
    {
        // Arrange
        var branch = new ConditionalBranch
        {
            Name = "Default",
            Condition = null,
            TargetNodeId = Guid.NewGuid()
        };

        // Act
        var isDefault = branch.IsDefaultBranch();

        // Assert
        isDefault.Should().BeTrue();
    }

    [Fact]
    public void IsDefaultBranch_ShouldReturnFalse_WhenConditionIsNotNull()
    {
        // Arrange
        var branch = new ConditionalBranch
        {
            Name = "Conditional",
            Condition = "quality == 'OK'",
            TargetNodeId = Guid.NewGuid()
        };

        // Act
        var isDefault = branch.IsDefaultBranch();

        // Assert
        isDefault.Should().BeFalse();
    }

    [Fact]
    public void IsDefaultBranch_ShouldReturnTrue_WhenConditionIsEmptyString()
    {
        // Arrange
        var branch = new ConditionalBranch
        {
            Name = "Default",
            Condition = "",
            TargetNodeId = Guid.NewGuid()
        };

        // Act
        var isDefault = branch.IsDefaultBranch();

        // Assert
        isDefault.Should().BeTrue();
    }

    [Fact]
    public void ConditionalBranch_ShouldHaveRecordEquality()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var branch1 = new ConditionalBranch
        {
            Name = "OK Branch",
            Condition = "quality == 'OK'",
            Priority = 1,
            TargetNodeId = targetId
        };
        var branch2 = new ConditionalBranch
        {
            Name = "OK Branch",
            Condition = "quality == 'OK'",
            Priority = 1,
            TargetNodeId = targetId
        };
        var branch3 = new ConditionalBranch
        {
            Name = "Different",
            Condition = "quality == 'OK'",
            Priority = 1,
            TargetNodeId = targetId
        };

        // Act & Assert
        branch1.Should().Be(branch2);
        branch1.Should().NotBe(branch3);
    }

    [Fact]
    public void ConditionalBranch_MultipleBranches_WithDifferentConditions()
    {
        // Arrange
        var okBranchId = Guid.NewGuid();
        var notOkBranchId = Guid.NewGuid();
        var defaultBranchId = Guid.NewGuid();

        // Act
        var branches = new List<ConditionalBranch>
        {
            new()
            {
                Name = "OK Branch",
                Condition = "quality_result == 'OK'",
                Priority = 1,
                TargetNodeId = okBranchId
            },
            new()
            {
                Name = "NotOK Branch",
                Condition = "quality_result == 'NotOK'",
                Priority = 2,
                TargetNodeId = notOkBranchId
            },
            new()
            {
                Name = "Default Branch",
                Condition = null,
                Priority = 999,
                TargetNodeId = defaultBranchId
            }
        };

        // Assert
        branches.Should().HaveCount(3);
        branches[0].Condition.Should().Be("quality_result == 'OK'");
        branches[1].Condition.Should().Be("quality_result == 'NotOK'");
        branches[2].IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void ConditionalBranch_WithComplexCondition()
    {
        // Arrange & Act
        var branch = new ConditionalBranch
        {
            Name = "Complex Condition",
            Condition = "temperature > 100 && pressure < 50 || quality == 'OK'",
            Priority = 5,
            TargetNodeId = Guid.NewGuid()
        };

        // Assert
        branch.Condition.Should().Be("temperature > 100 && pressure < 50 || quality == 'OK'");
        branch.IsDefaultBranch().Should().BeFalse();
    }

    [Fact]
    public void ConditionalBranch_WithWhitespaceCondition_ShouldNotBeDefault()
    {
        // Arrange & Act
        var branch = new ConditionalBranch
        {
            Name = "Whitespace",
            Condition = "   ",
            TargetNodeId = Guid.NewGuid()
        };

        // Assert
        // Note: IsDefaultBranch uses string.IsNullOrEmpty, so whitespace is NOT considered default
        branch.IsDefaultBranch().Should().BeFalse();
    }

    [Fact]
    public void ConditionalBranch_SameName_DifferentCondition_ShouldNotBeEqual()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var branch1 = new ConditionalBranch
        {
            Name = "Branch",
            Condition = "condition1",
            TargetNodeId = targetId
        };
        var branch2 = new ConditionalBranch
        {
            Name = "Branch",
            Condition = "condition2",
            TargetNodeId = targetId
        };

        // Act & Assert
        branch1.Should().NotBe(branch2);
    }

    [Fact]
    public void ConditionalBranch_SameEverythingExceptTargetNode_ShouldNotBeEqual()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var node1 = new TaskNode
        {
            Id = targetId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = "Task1",
                Description = "",
                StartTime = 0,
                Duration = 10
            },
            ProcedureId = default
        };
        var node2 = new TaskNode
        {
            Id = targetId,
            Position = new NodePosition
            {
                X = 100,
                Y = 100
            },
            Task = new Task
            {
                Name = "Task2",
                Description = "",
                StartTime = 0,
                Duration = 20
            },
            ProcedureId = default
        };

        var branch1 = new ConditionalBranch
        {
            Name = "Branch",
            Condition = "test",
            Priority = 1,
            TargetNodeId = targetId,
            TargetNode = node1
        };
        var branch2 = new ConditionalBranch
        {
            Name = "Branch",
            Condition = "test",
            Priority = 1,
            TargetNodeId = targetId,
            TargetNode = node2
        };

        // Act & Assert
        // Even though TargetNode is a navigation typedProperty (not persisted),
        // records include all properties in equality comparison
        branch1.Should().NotBe(branch2);
    }
}