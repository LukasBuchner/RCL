using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Domain.Procedure;

/// <summary>
///     Tests for RouterNode with conditional branching (Selector and Branches in RouterTask).
/// </summary>
public class RouterNodeBranchingTests
{
    [Fact]
    public void TaskNode_ShouldCreateWithoutSelector_SimpleComposite()
    {
        // Arrange & Act
        var taskNode = new TaskNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 100,
                Y = 200
            },
            Task = new Task
            {
                Name = "Composite Task",
                Description = "Groups child tasks",
                StartTime = 0,
                Duration = 10
            },
            ProcedureId = default
        };

        // Assert
        taskNode.Task.Name.Should().Be("Composite Task");
    }

    [Fact]
    public void RouterNode_ShouldCreateWithSelector_Router()
    {
        // Arrange & Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 100,
                Y = 200
            },
            RouterTask = new RouterTask
            {
                Name = "Router Task",
                Description = "Routes based on condition",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Selector.Should().NotBeNull();
        routerNode.RouterTask.Selector.Should().BeOfType<SimpleVariableSelector>();
        routerNode.RouterTask.Selector!.Expression.Should().Be("quality_result");
    }

    [Fact]
    public void RouterNode_ShouldCreateWithSelectorAndBranches()
    {
        // Arrange
        var okBranchId = Guid.NewGuid();
        var notOkBranchId = Guid.NewGuid();

        // Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 100,
                Y = 200
            },
            RouterTask = new RouterTask
            {
                Name = "Quality Router",
                Description = "Routes based on quality result",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>
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
                    }
                }
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Selector.Should().NotBeNull();
        routerNode.RouterTask.Branches.Should().NotBeNull();
        routerNode.RouterTask.Branches!.Should().HaveCount(2);
        routerNode.RouterTask.Branches[0].Name.Should().Be("OK Branch");
        routerNode.RouterTask.Branches[1].Name.Should().Be("NotOK Branch");
    }

    [Fact]
    public void RouterNode_WithSimpleVariableSelector()
    {
        // Arrange & Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Simple Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Selector.Should().BeOfType<SimpleVariableSelector>();
        var simpleSelector = (SimpleVariableSelector)routerNode.RouterTask.Selector!;
        simpleSelector.VariableName.Should().Be("quality_result");
    }

    [Fact]
    public void RouterNode_WithExpressionSelector()
    {
        // Arrange & Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Expression Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new ExpressionSelector
                {
                    Expression = "temperature > 100 && pressure < 50"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Selector.Should().BeOfType<ExpressionSelector>();
        routerNode.RouterTask.Selector!.Expression.Should().Be("temperature > 100 && pressure < 50");
    }

    [Fact]
    public void RouterNode_WithExpressionSelector_ComplexExpression()
    {
        // Arrange & Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Complex Expression Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new ExpressionSelector
                {
                    Expression = "Math.Max(temp1, temp2) > threshold"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Selector.Should().BeOfType<ExpressionSelector>();
        routerNode.RouterTask.Selector!.Expression.Should().Be("Math.Max(temp1, temp2) > threshold");
    }

    [Fact]
    public void RouterNode_MultipleBranches_ShouldStore()
    {
        // Arrange
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var branch3Id = Guid.NewGuid();

        // Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Multi-Branch Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "status"
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Success",
                        Condition = "status == 'success'",
                        Priority = 1,
                        TargetNodeId = branch1Id
                    },
                    new()
                    {
                        Name = "Warning",
                        Condition = "status == 'warning'",
                        Priority = 2,
                        TargetNodeId = branch2Id
                    },
                    new()
                    {
                        Name = "Error",
                        Condition = "status == 'error'",
                        Priority = 3,
                        TargetNodeId = branch3Id
                    }
                }
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Branches.Should().HaveCount(3);
        routerNode.RouterTask.Branches![0].Name.Should().Be("Success");
        routerNode.RouterTask.Branches[1].Name.Should().Be("Warning");
        routerNode.RouterTask.Branches[2].Name.Should().Be("Error");
    }

    [Fact]
    public void RouterNode_WithDefaultBranch()
    {
        // Arrange
        var okBranchId = Guid.NewGuid();
        var defaultBranchId = Guid.NewGuid();

        // Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router with Default",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>
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
                        Name = "Default Branch",
                        Condition = null,
                        Priority = 999,
                        TargetNodeId = defaultBranchId
                    }
                }
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Branches.Should().HaveCount(2);
        routerNode.RouterTask.Branches![0].IsDefaultBranch().Should().BeFalse();
        routerNode.RouterTask.Branches[1].IsDefaultBranch().Should().BeTrue();
    }

    [Fact]
    public void RouterNode_WithMultipleNonDefaultBranches()
    {
        // Arrange
        var branches = Enumerable.Range(1, 5)
            .Select(i => new ConditionalBranch
            {
                Name = $"Branch {i}",
                Condition = $"value == {i}",
                Priority = i,
                TargetNodeId = Guid.NewGuid()
            })
            .ToList();

        // Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Multi-Branch Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "value"
                },
                Branches = branches
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Branches.Should().HaveCount(5);
        routerNode.RouterTask.Branches!.All(b => !b.IsDefaultBranch()).Should().BeTrue();
    }

    [Fact]
    public void RouterNode_RecordEquality_WithSelector()
    {
        // Arrange
        var id = Guid.NewGuid();
        // Use the same list instance for both nodes since List<T> uses reference equality in records
        var branches = new List<ConditionalBranch>();
        var routerNode1 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = branches
            },
            ProcedureId = default
        };
        var routerNode2 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = branches
            },
            ProcedureId = default
        };

        // Act & Assert
        routerNode1.Should().Be(routerNode2);
    }

    [Fact]
    public void RouterNode_RecordEquality_WithSelectorAndBranches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var branches = new List<ConditionalBranch>
        {
            new()
            {
                Name = "OK",
                Condition = "quality_result == 'OK'",
                Priority = 1,
                TargetNodeId = branchId
            }
        };

        var routerNode1 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = branches
            },
            ProcedureId = default
        };
        var routerNode2 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = branches
            },
            ProcedureId = default
        };

        // Act & Assert
        routerNode1.Should().Be(routerNode2);
    }

    [Fact]
    public void RouterNode_DifferentSelector_ShouldNotBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var routerNode1 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };
        var routerNode2 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "different_variable"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Act & Assert
        routerNode1.Should().NotBe(routerNode2);
    }

    [Fact]
    public void RouterNode_PolymorphicSelector_SimpleVsExpression_ShouldNotBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var routerNode1 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "test"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };
        var routerNode2 = new RouterNode
        {
            Id = id,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "",
                StartTime = 0,
                Duration = 0,
                Selector = new ExpressionSelector
                {
                    Expression = "test"
                },
                Branches = new List<ConditionalBranch>()
            },
            ProcedureId = default
        };

        // Act & Assert
        routerNode1.Should().NotBe(routerNode2);
    }

    [Fact]
    public void RouterNode_RouterWithZeroDuration_IsValid()
    {
        // Arrange & Act
        var routerNode = new RouterNode
        {
            Id = Guid.NewGuid(),
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            RouterTask = new RouterTask
            {
                Name = "Router",
                Description = "Routers typically have 0 duration",
                StartTime = 10,
                Duration = 0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "quality_result"
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Default",
                        Condition = null,
                        Priority = 999,
                        TargetNodeId = Guid.NewGuid()
                    }
                }
            },
            ProcedureId = default
        };

        // Assert
        routerNode.RouterTask.Duration.Should().Be(0);
        routerNode.RouterTask.Selector.Should().NotBeNull();
        routerNode.RouterTask.Branches.Should().HaveCount(1);
    }
}