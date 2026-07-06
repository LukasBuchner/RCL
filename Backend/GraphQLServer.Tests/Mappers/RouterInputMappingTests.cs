using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using FluentAssertions;

namespace FHOOE.Freydis.GraphQLServer.Tests.Mappers;

/// <summary>
///     Tests for mapping router-related GraphQL input types to domain entities.
///     Validates selector expressions and conditional branches transformation.
/// </summary>
public class RouterInputMappingTests
{
    [Fact]
    public void MapFromSelectorInput_WithSimpleVariableSelector_MapsCorrectly()
    {
        // Arrange
        var input = new SelectorExpressionInput
        {
            SimpleVariableSelector = new SimpleVariableSelectorInput
            {
                Expression = "qualityResult"
            }
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromSelectorInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Should().BeOfType<SimpleVariableSelector>();
        domain.Expression.Should().Be("qualityResult");
        var simpleSelector = domain as SimpleVariableSelector;
        simpleSelector!.VariableName.Should().Be("qualityResult");
    }

    [Fact]
    public void MapFromSelectorInput_WithExpressionSelector_MapsCorrectly()
    {
        // Arrange
        var input = new SelectorExpressionInput
        {
            ExpressionSelector = new ExpressionSelectorInput
            {
                Expression = "temperature > 100 && pressure < 50"
            }
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromSelectorInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Should().BeOfType<ExpressionSelector>();
        domain.Expression.Should().Be("temperature > 100 && pressure < 50");
    }

    [Fact]
    public void MapFromSelectorInput_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        SelectorExpressionInput? input = null;

        // Act
        Action act = () => GraphQlDtoMapperService.MapFromSelectorInput(input!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapFromSelectorInput_WithNoSelectorSet_ThrowsArgumentException()
    {
        // Arrange - OneOf input with no selector set (invalid)
        var input = new SelectorExpressionInput();

        // Act
        Action act = () => GraphQlDtoMapperService.MapFromSelectorInput(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("No valid selector type set in SelectorExpressionInput.*");
    }

    [Fact]
    public void MapFromConditionalBranchInput_WithCondition_MapsCorrectly()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var input = new ConditionalBranchInput
        {
            Name = "High Quality",
            Condition = "qualityResult == 'OK'",
            Priority = 1,
            TargetNodeId = targetNodeId
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromConditionalBranchInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Name.Should().Be("High Quality");
        domain.Condition.Should().Be("qualityResult == 'OK'");
        domain.Priority.Should().Be(1);
        domain.TargetNodeId.Should().Be(targetNodeId);
        domain.IsDefaultBranch().Should().BeFalse();
    }

    [Fact]
    public void MapFromConditionalBranchInput_AsDefaultBranch_MapsCorrectly()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var input = new ConditionalBranchInput
        {
            Name = "Default",
            Condition = null, // null condition = default branch
            Priority = 999,
            TargetNodeId = targetNodeId
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromConditionalBranchInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Name.Should().Be("Default");
        domain.Condition.Should().BeNull();
        domain.Priority.Should().Be(999);
        domain.TargetNodeId.Should().Be(targetNodeId);
        domain.IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void MapFromConditionalBranchInput_WithEmptyCondition_TreatsAsDefaultBranch()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var input = new ConditionalBranchInput
        {
            Name = "Default",
            Condition = "", // empty string = default branch
            Priority = 999,
            TargetNodeId = targetNodeId
        };

        // Act
        var domain = GraphQlDtoMapperService.MapFromConditionalBranchInput(input);

        // Assert
        domain.Should().NotBeNull();
        domain.Condition.Should().BeEmpty();
        domain.IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void MapFromConditionalBranchInput_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        ConditionalBranchInput? input = null;

        // Act
        Action act = () => GraphQlDtoMapperService.MapFromConditionalBranchInput(input!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapToRouterNodeDto_WithSelectorAndBranches_MapsCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var targetNodeId1 = Guid.NewGuid();
        var targetNodeId2 = Guid.NewGuid();

        var input = new NodeInput
        {
            RouterNode = new RouterNodeInput
            {
                Id = nodeId,
                Position = new NodePosition { X = 100, Y = 200 },
                ParentId = null,
                RouterTaskInput = new RouterTaskInput
                {
                    Name = "Quality Router",
                    Description = "Routes based on quality result",
                    StartTime = 0.0,
                    Duration = 1.0,
                    Selector = new SelectorExpressionInput
                    {
                        SimpleVariableSelector = new SimpleVariableSelectorInput
                        {
                            Expression = "qualityResult"
                        }
                    },
                    Branches = new List<ConditionalBranchInput>
                    {
                        new()
                        {
                            Name = "OK Path",
                            Condition = "qualityResult == 'OK'",
                            Priority = 1,
                            TargetNodeId = targetNodeId1
                        },
                        new()
                        {
                            Name = "Default Path",
                            Condition = null,
                            Priority = 999,
                            TargetNodeId = targetNodeId2
                        }
                    }
                }
            }
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToNodeDto(input);

        // Assert
        dto.Should().NotBeNull();
        dto.RouterNode.Should().NotBeNull();
        dto.RouterNode!.Id.Should().Be(nodeId);
        dto.RouterNode.RouterTask.Name.Should().Be("Quality Router");

        // Verify selector
        dto.RouterNode.RouterTask.Selector.Should().NotBeNull();
        dto.RouterNode.RouterTask.Selector.Should().BeOfType<SimpleVariableSelector>();
        dto.RouterNode.RouterTask.Selector!.Expression.Should().Be("qualityResult");

        // Verify branches
        dto.RouterNode.RouterTask.Branches.Should().NotBeNull();
        dto.RouterNode.RouterTask.Branches.Should().HaveCount(2);
        dto.RouterNode.RouterTask.Branches![0].Name.Should().Be("OK Path");
        dto.RouterNode.RouterTask.Branches[0].Condition.Should().Be("qualityResult == 'OK'");
        dto.RouterNode.RouterTask.Branches[0].Priority.Should().Be(1);
        dto.RouterNode.RouterTask.Branches[0].TargetNodeId.Should().Be(targetNodeId1);
        dto.RouterNode.RouterTask.Branches[1].Name.Should().Be("Default Path");
        dto.RouterNode.RouterTask.Branches[1].Condition.Should().BeNull();
        dto.RouterNode.RouterTask.Branches[1].IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void MapToTaskNodeDto_WithoutSelector_CreatesSimpleCompositeTask()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var input = new NodeInput
        {
            TaskNode = new TaskNodeInput
            {
                Id = nodeId,
                Position = new NodePosition { X = 100, Y = 200 },
                ParentId = null,
                TaskInput = new TaskInput
                {
                    Name = "Regular Task",
                    Description = "Non-router task",
                    StartTime = 0.0,
                    Duration = 5.0
                }
            }
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToNodeDto(input);

        // Assert
        dto.Should().NotBeNull();
        dto.TaskNode.Should().NotBeNull();
        dto.TaskNode!.Id.Should().Be(nodeId);
        dto.TaskNode.Task.Name.Should().Be("Regular Task");
        dto.RouterNode.Should().BeNull(); // Not a router
    }

    [Fact]
    public void MapToRouterNodeDto_WithExpressionSelector_MapsCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var targetNodeId = Guid.NewGuid();

        var input = new NodeInput
        {
            RouterNode = new RouterNodeInput
            {
                Id = nodeId,
                Position = new NodePosition { X = 100, Y = 200 },
                RouterTaskInput = new RouterTaskInput
                {
                    Name = "Temperature Router",
                    StartTime = 0.0,
                    Duration = 1.0,
                    Selector = new SelectorExpressionInput
                    {
                        ExpressionSelector = new ExpressionSelectorInput
                        {
                            Expression = "temperature > 100 && pressure < 50"
                        }
                    },
                    Branches = new List<ConditionalBranchInput>
                    {
                        new()
                        {
                            Name = "High Temp",
                            Condition = "temperature > 100",
                            Priority = 1,
                            TargetNodeId = targetNodeId
                        }
                    }
                }
            }
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToNodeDto(input);

        // Assert
        dto.Should().NotBeNull();
        dto.RouterNode.Should().NotBeNull();
        dto.RouterNode!.RouterTask.Selector.Should().BeOfType<ExpressionSelector>();
        dto.RouterNode.RouterTask.Selector!.Expression.Should().Be("temperature > 100 && pressure < 50");
    }

    [Fact]
    public void MapToConditionalBranchDto_FromDomain_MapsCorrectly()
    {
        // Arrange
        var targetNodeId = Guid.NewGuid();
        var domainBranch = new ConditionalBranch
        {
            Name = "OK Branch",
            Condition = "result == 'OK'",
            Priority = 1,
            TargetNodeId = targetNodeId
        };

        // Act
        var dto = GraphQlDtoMapperService.MapToConditionalBranchDto(domainBranch);

        // Assert
        dto.Should().NotBeNull();
        dto.Name.Should().Be("OK Branch");
        dto.Condition.Should().Be("result == 'OK'");
        dto.Priority.Should().Be(1);
        dto.TargetNodeId.Should().Be(targetNodeId);
    }

    [Fact]
    public void MapToConditionalBranchDto_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        ConditionalBranch? domainBranch = null;

        // Act
        Action act = () => GraphQlDtoMapperService.MapToConditionalBranchDto(domainBranch!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapFromConditionalBranchInputs_WithMultipleBranches_MapsAllCorrectly()
    {
        // Arrange
        var targetId1 = Guid.NewGuid();
        var targetId2 = Guid.NewGuid();
        var targetId3 = Guid.NewGuid();

        var inputs = new List<ConditionalBranchInput>
        {
            new() { Name = "Branch 1", Condition = "var == 'A'", Priority = 1, TargetNodeId = targetId1 },
            new() { Name = "Branch 2", Condition = "var == 'B'", Priority = 2, TargetNodeId = targetId2 },
            new() { Name = "Default", Condition = null, Priority = 999, TargetNodeId = targetId3 }
        };

        // Act
        var domainBranches = inputs.Select(GraphQlDtoMapperService.MapFromConditionalBranchInput).ToList();

        // Assert
        domainBranches.Should().HaveCount(3);
        domainBranches[0].Name.Should().Be("Branch 1");
        domainBranches[0].Condition.Should().Be("var == 'A'");
        domainBranches[1].Name.Should().Be("Branch 2");
        domainBranches[2].IsDefaultBranch().Should().BeTrue();
    }
}