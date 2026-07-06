using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Filtering;

public class RouterBranchFilterServiceTests
{
    private readonly ILogger<RouterBranchFilterService> _logger = Mock.Of<ILogger<RouterBranchFilterService>>();

    [Fact]
    public async Task FilterNodesAsync_NoRouters_ReturnsAllNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var task1 = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Task1", StartTime = 0, Duration = 10 }
        };
        var task2 = new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Task2", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { task1, task2 };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().HaveCount(2);
        result.IncludedNodes.Should().Contain(task1);
        result.IncludedNodes.Should().Contain(task2);
        result.ExcludedNodes.Should().BeEmpty();
        result.RouterSelections.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithSelectedBranch_IncludesOnlySelectedBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Quality Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "OK", TargetNodeId = branch1Id },
                    new() { Name = "NotOK", TargetNodeId = branch2Id }
                },
                SelectedBranchTargetNodeId = branch1Id,
                SelectedBranchName = "OK"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "OK Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "NotOK Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().NotContain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(branch2TaskNode);
        result.RouterSelections.Should().ContainKey(routerId);
        result.RouterSelections[routerId].SelectedBranchName.Should().Be("OK");
        result.RouterSelections[routerId].SelectedBranchTargetNodeId.Should().Be(branch1Id);
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithSelectedBranch_ExcludesOtherBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Status Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "status" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Success", TargetNodeId = branch1Id },
                    new() { Name = "Failure", TargetNodeId = branch2Id }
                },
                SelectedBranchTargetNodeId = branch2Id,
                SelectedBranchName = "Failure"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Success Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Failure Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().NotContain(branch1TaskNode);
        result.ExcludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().NotContain(branch2TaskNode);
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithSelectedBranch_IncludesDescendantsOfSelectedBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var grandchild1Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Path Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "path" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "PathA", TargetNodeId = branch1Id },
                    new() { Name = "PathB", TargetNodeId = branch2Id }
                },
                SelectedBranchTargetNodeId = branch1Id,
                SelectedBranchName = "PathA"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "PathA Root", StartTime = 0, Duration = 10 }
        };

        var child1TaskNode = new TaskNode
        {
            Id = child1Id,
            ParentId = branch1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "PathA Child", StartTime = 0, Duration = 10 }
        };

        var grandchild1TaskNode = new TaskNode
        {
            Id = grandchild1Id,
            ParentId = child1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "PathA Grandchild", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "PathB Root", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node>
            { routerNode, branch1TaskNode, child1TaskNode, grandchild1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(child1TaskNode);
        result.IncludedNodes.Should().Contain(grandchild1TaskNode);
        result.ExcludedNodes.Should().NotContain(branch1TaskNode);
        result.ExcludedNodes.Should().NotContain(child1TaskNode);
        result.ExcludedNodes.Should().NotContain(grandchild1TaskNode);
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithSelectedBranch_ExcludesDescendantsOfNonSelectedBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();
        var grandchild2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Mode Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "mode" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Auto", TargetNodeId = branch1Id },
                    new() { Name = "Manual", TargetNodeId = branch2Id }
                },
                SelectedBranchTargetNodeId = branch1Id,
                SelectedBranchName = "Auto"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Auto Root", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Manual Root", StartTime = 0, Duration = 10 }
        };

        var child2TaskNode = new TaskNode
        {
            Id = child2Id,
            ParentId = branch2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Manual Child", StartTime = 0, Duration = 10 }
        };

        var grandchild2TaskNode = new TaskNode
        {
            Id = grandchild2Id,
            ParentId = child2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Manual Grandchild", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node>
            { routerNode, branch1TaskNode, branch2TaskNode, child2TaskNode, grandchild2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.ExcludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(child2TaskNode);
        result.ExcludedNodes.Should().Contain(grandchild2TaskNode);
        result.IncludedNodes.Should().NotContain(branch2TaskNode);
        result.IncludedNodes.Should().NotContain(child2TaskNode);
        result.IncludedNodes.Should().NotContain(grandchild2TaskNode);
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithoutSelection_IncludesAllBranches()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Design-time Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "choice" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "OptionA", TargetNodeId = branch1Id },
                    new() { Name = "OptionB", TargetNodeId = branch2Id }
                }
                // No SelectedBranchTargetNodeId - design-time mode
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "OptionA Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "OptionB Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().BeEmpty();
        result.RouterSelections.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterNodesAsync_MultipleRouters_FiltersEachIndependently()
    {
        // Arrange
        var procedureId = Guid.NewGuid();

        var router1Id = Guid.NewGuid();
        var router1Branch1Id = Guid.NewGuid();
        var router1Branch2Id = Guid.NewGuid();

        var router2Id = Guid.NewGuid();
        var router2Branch1Id = Guid.NewGuid();
        var router2Branch2Id = Guid.NewGuid();

        var router1Node = new RouterNode
        {
            Id = router1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "First Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "first" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "A", TargetNodeId = router1Branch1Id },
                    new() { Name = "B", TargetNodeId = router1Branch2Id }
                },
                SelectedBranchTargetNodeId = router1Branch1Id,
                SelectedBranchName = "A"
            }
        };

        var router2Node = new RouterNode
        {
            Id = router2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Second Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "second" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "X", TargetNodeId = router2Branch1Id },
                    new() { Name = "Y", TargetNodeId = router2Branch2Id }
                },
                SelectedBranchTargetNodeId = router2Branch2Id,
                SelectedBranchName = "Y"
            }
        };

        var r1B1 = new TaskNode
        {
            Id = router1Branch1Id,
            ParentId = router1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "R1-A", StartTime = 0, Duration = 10 }
        };

        var r1B2 = new TaskNode
        {
            Id = router1Branch2Id,
            ParentId = router1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "R1-B", StartTime = 0, Duration = 10 }
        };

        var r2B1 = new TaskNode
        {
            Id = router2Branch1Id,
            ParentId = router2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "R2-X", StartTime = 0, Duration = 10 }
        };

        var r2B2 = new TaskNode
        {
            Id = router2Branch2Id,
            ParentId = router2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "R2-Y", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { router1Node, r1B1, r1B2, router2Node, r2B1, r2B2 };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(r1B1);
        result.ExcludedNodes.Should().Contain(r1B2);
        result.IncludedNodes.Should().Contain(r2B2);
        result.ExcludedNodes.Should().Contain(r2B1);
        result.RouterSelections.Should().HaveCount(2);
        result.RouterSelections[router1Id].SelectedBranchName.Should().Be("A");
        result.RouterSelections[router2Id].SelectedBranchName.Should().Be("Y");
    }

    [Fact]
    public async Task FilterNodesAsync_RouterWithThreeBranches_IncludesOnlySelected()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var branch3Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Three-way Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "priority" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "High", TargetNodeId = branch1Id },
                    new() { Name = "Medium", TargetNodeId = branch2Id },
                    new() { Name = "Low", TargetNodeId = branch3Id }
                },
                SelectedBranchTargetNodeId = branch2Id,
                SelectedBranchName = "Medium"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "High Priority", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Medium Priority", StartTime = 0, Duration = 10 }
        };

        var branch3TaskNode = new TaskNode
        {
            Id = branch3Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Low Priority", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode, branch3TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(branch1TaskNode);
        result.ExcludedNodes.Should().Contain(branch3TaskNode);
        result.ExcludedNodes.Should().HaveCount(2);
        result.RouterSelections[routerId].SelectedBranchName.Should().Be("Medium");
    }

    [Fact]
    public async Task FilterNodesAsync_EmptyNodeList_ReturnsEmptyResult()
    {
        // Arrange
        var nodes = new List<Node>();
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().BeEmpty();
        result.ExcludedNodes.Should().BeEmpty();
        result.RouterSelections.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterNodesAsync_NestedDescendants_IncludesAllGenerations()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var gen2Id = Guid.NewGuid();
        var gen3Id = Guid.NewGuid();
        var gen4Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Deep Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "depth" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Deep", TargetNodeId = branch1Id },
                    new() { Name = "Shallow", TargetNodeId = branch2Id }
                },
                SelectedBranchTargetNodeId = branch1Id,
                SelectedBranchName = "Deep"
            }
        };

        var branch1 = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Gen 1", StartTime = 0, Duration = 10 }
        };

        var gen2 = new TaskNode
        {
            Id = gen2Id,
            ParentId = branch1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Gen 2", StartTime = 0, Duration = 10 }
        };

        var gen3 = new TaskNode
        {
            Id = gen3Id,
            ParentId = gen2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Gen 3", StartTime = 0, Duration = 10 }
        };

        var gen4 = new TaskNode
        {
            Id = gen4Id,
            ParentId = gen3Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Gen 4", StartTime = 0, Duration = 10 }
        };

        var branch2 = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Shallow", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1, gen2, gen3, gen4, branch2 };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(branch1);
        result.IncludedNodes.Should().Contain(gen2);
        result.IncludedNodes.Should().Contain(gen3);
        result.IncludedNodes.Should().Contain(gen4);
        result.ExcludedNodes.Should().Contain(branch2);
        result.ExcludedNodes.Should().HaveCount(1);
    }

    #region Phase 2: Manual Branch Selection Tests

    [Fact]
    public async Task FilterNodesAsync_WithManuallySelectedBranch_FiltersToSelectedBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Design Mode Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "OK", TargetNodeId = branch1Id },
                    new() { Name = "NotOK", TargetNodeId = branch2Id }
                },
                ManuallySelectedBranch = "OK" // User manually selected "OK" branch in design mode
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "OK Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "NotOK Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().NotContain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(branch2TaskNode);
        result.RouterSelections.Should().ContainKey(routerId);
        result.RouterSelections[routerId].SelectedBranchName.Should().Be("OK");
        result.RouterSelections[routerId].SelectedBranchTargetNodeId.Should().Be(branch1Id);
        result.RouterSelections[routerId].Reason.Should().Be("Manual selection");
    }

    [Fact]
    public async Task FilterNodesAsync_WithBothExecutionAndManualSelection_PrefersExecutionState()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Priority Test Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "result" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Pass", TargetNodeId = branch1Id },
                    new() { Name = "Fail", TargetNodeId = branch2Id }
                },
                ManuallySelectedBranch = "Pass", // User manually selected "Pass"
                SelectedBranchTargetNodeId = branch2Id, // Execution selected "Fail"
                SelectedBranchName = "Fail"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Pass Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Fail Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert - Execution state should win (Fail branch included)
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().NotContain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(branch1TaskNode);
        result.ExcludedNodes.Should().NotContain(branch2TaskNode);
        result.RouterSelections.Should().ContainKey(routerId);
        result.RouterSelections[routerId].SelectedBranchName.Should().Be("Fail");
        result.RouterSelections[routerId].SelectedBranchTargetNodeId.Should().Be(branch2Id);
        result.RouterSelections[routerId].Reason.Should().Be("Execution state");
    }

    [Fact]
    public async Task FilterNodesAsync_WithNoSelection_ShowsAllBranches()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "No Selection Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "choice" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Option1", TargetNodeId = branch1Id },
                    new() { Name = "Option2", TargetNodeId = branch2Id }
                }
                // No ManuallySelectedBranch and no SelectedBranchTargetNodeId
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Option1 Branch", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Option2 Branch", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node> { routerNode, branch1TaskNode, branch2TaskNode };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert - All branches should be visible
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().BeEmpty();
        result.RouterSelections.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterNodesAsync_WithManualSelection_FiltersChildrenOfSelectedBranch()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var grandchild1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        var routerNode = new RouterNode
        {
            Id = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Design Selection Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "path" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "ComplexPath", TargetNodeId = branch1Id },
                    new() { Name = "SimplePath", TargetNodeId = branch2Id }
                },
                ManuallySelectedBranch = "ComplexPath"
            }
        };

        var branch1TaskNode = new TaskNode
        {
            Id = branch1Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Complex Root", StartTime = 0, Duration = 10 }
        };

        var child1TaskNode = new TaskNode
        {
            Id = child1Id,
            ParentId = branch1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Complex Child", StartTime = 0, Duration = 10 }
        };

        var grandchild1TaskNode = new TaskNode
        {
            Id = grandchild1Id,
            ParentId = child1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Complex Grandchild", StartTime = 0, Duration = 10 }
        };

        var branch2TaskNode = new TaskNode
        {
            Id = branch2Id,
            ParentId = routerId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Simple Root", StartTime = 0, Duration = 10 }
        };

        var child2TaskNode = new TaskNode
        {
            Id = child2Id,
            ParentId = branch2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Simple Child", StartTime = 0, Duration = 10 }
        };

        var nodes = new List<Node>
        {
            routerNode, branch1TaskNode, child1TaskNode, grandchild1TaskNode,
            branch2TaskNode, child2TaskNode
        };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert - ComplexPath and all its descendants should be included
        result.IncludedNodes.Should().Contain(routerNode);
        result.IncludedNodes.Should().Contain(branch1TaskNode);
        result.IncludedNodes.Should().Contain(child1TaskNode);
        result.IncludedNodes.Should().Contain(grandchild1TaskNode);
        result.IncludedNodes.Should().NotContain(branch2TaskNode);
        result.IncludedNodes.Should().NotContain(child2TaskNode);
        result.ExcludedNodes.Should().Contain(branch2TaskNode);
        result.ExcludedNodes.Should().Contain(child2TaskNode);
        result.RouterSelections[routerId].SelectedBranchName.Should().Be("ComplexPath");
        result.RouterSelections[routerId].Reason.Should().Be("Manual selection");
    }

    /// <summary>
    ///     Reproduces the exact production topology (12 nodes) where an outer router and inner
    ///     router both have ManuallySelectedBranch set. The hierarchy includes SkillExecutionNodes
    ///     and root-level orphan skills, which the simpler existing test
    ///     <see cref="FilterNodesAsync_NestedRouterBothWithSelection_ExcludesInnerNonSelectedBranch"/>
    ///     does not cover.
    ///     <para>
    ///         Production hierarchy:
    ///         <code>
    ///             rfg (OuterRouter, root)                            — ManuallySelectedBranch="Default"
    ///             ├── OK Branch (Task, parent=rfg)
    ///             │    └── skill-in-ok-branch (SkillExecution)
    ///             └── Default Branch (Task, parent=rfg)
    ///                  ├── Another Router (InnerRouter, parent=Default Branch) — ManuallySelectedBranch="Branch 1"
    ///                  │    ├── Branch 1 Branch (Task, parent=Another Router) → skill-in-branch1
    ///                  │    └── Default Branch inner (Task, parent=Another Router) → skill-in-inner-default
    ///                  └── skill-in-default-branch (SkillExecution)
    ///             root-skill-1 (SkillExecution, no parent)
    ///             root-skill-2 (SkillExecution, no parent)
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Expected: 8 included (rfg, Default Branch, Another Router, Branch 1 Branch,
    ///         skill-in-branch1, skill-in-default-branch, root-skill-1, root-skill-2).
    ///         Expected: 4 excluded (OK Branch, skill-in-ok-branch, Default Branch inner,
    ///         skill-in-inner-default).
    ///     </para>
    /// </summary>
    [Fact]
    public async Task FilterNodesAsync_ProductionScenario_NestedRouterWithSkillDescendants_ManualBothSelected()
    {
        // Arrange — build the exact 12-node production topology
        var procedureId = Guid.NewGuid();

        // IDs for all nodes
        var outerRouterId = Guid.NewGuid(); // rfg
        var okBranchId = Guid.NewGuid(); // OK Branch
        var skillInOkBranchId = Guid.NewGuid(); // skill-in-ok-branch
        var defaultBranchId = Guid.NewGuid(); // Default Branch
        var innerRouterId = Guid.NewGuid(); // Another Router
        var branch1BranchId = Guid.NewGuid(); // Branch 1 Branch
        var skillInBranch1Id = Guid.NewGuid(); // skill-in-branch1
        var innerDefaultBranchId = Guid.NewGuid(); // Default Branch inner
        var skillInInnerDefaultId = Guid.NewGuid(); // skill-in-inner-default
        var skillInDefaultBranchId = Guid.NewGuid(); // skill-in-default-branch
        var rootSkill1Id = Guid.NewGuid(); // root-skill-1
        var rootSkill2Id = Guid.NewGuid(); // root-skill-2

        // Outer router: ManuallySelectedBranch="Default" (Priority 2 — no SelectedBranchTargetNodeId)
        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "rfg",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality_result" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "OK", TargetNodeId = okBranchId, Priority = 0, Condition = "quality_result == 'OK'"
                    },
                    new() { Name = "Default", TargetNodeId = defaultBranchId, Priority = 1 }
                },
                ManuallySelectedBranch = "Default"
            }
        };

        var okBranch = new TaskNode
        {
            Id = okBranchId,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "OK Branch", StartTime = 0, Duration = 10 }
        };

        var skillInOkBranch = new SkillExecutionNode
        {
            Id = skillInOkBranchId,
            ParentId = okBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Skill in OK",
                StartTime = 0,
                Duration = 5,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Skill in OK", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var defaultBranch = new TaskNode
        {
            Id = defaultBranchId,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Default Branch", StartTime = 0, Duration = 100 }
        };

        // Inner router: ManuallySelectedBranch="Branch 1" (Priority 2 — no SelectedBranchTargetNodeId)
        var innerRouter = new RouterNode
        {
            Id = innerRouterId,
            ParentId = defaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Another Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "inner_var" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Branch 1", TargetNodeId = branch1BranchId, Priority = 0, Condition = "inner_var == 1"
                    },
                    new() { Name = "Default", TargetNodeId = innerDefaultBranchId, Priority = 1 }
                },
                ManuallySelectedBranch = "Branch 1"
            }
        };

        var branch1Branch = new TaskNode
        {
            Id = branch1BranchId,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Branch 1 Branch", StartTime = 0, Duration = 30 }
        };

        var skillInBranch1 = new SkillExecutionNode
        {
            Id = skillInBranch1Id,
            ParentId = branch1BranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Move To Position Tag",
                StartTime = 0,
                Duration = 15,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position Tag",
                    Description = "",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };

        var innerDefaultBranch = new TaskNode
        {
            Id = innerDefaultBranchId,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner Default Branch", StartTime = 0, Duration = 40 }
        };

        var skillInInnerDefault = new SkillExecutionNode
        {
            Id = skillInInnerDefaultId,
            ParentId = innerDefaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Release Object",
                StartTime = 0,
                Duration = 10,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Release Object", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var skillInDefaultBranch = new SkillExecutionNode
        {
            Id = skillInDefaultBranchId,
            ParentId = defaultBranchId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Default Branch Skill",
                StartTime = 0,
                Duration = 20,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Branch Skill",
                    Description = "",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };

        var rootSkill1 = new SkillExecutionNode
        {
            Id = rootSkill1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Root Skill 1",
                StartTime = 0,
                Duration = 25,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Root Skill 1", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var rootSkill2 = new SkillExecutionNode
        {
            Id = rootSkill2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Root Skill 2",
                StartTime = 0,
                Duration = 25,
                Skill = new Skill
                { Id = Guid.NewGuid(), Name = "Root Skill 2", Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };

        var nodes = new List<Node>
        {
            outerRouter, okBranch, skillInOkBranch, defaultBranch,
            innerRouter, branch1Branch, skillInBranch1,
            innerDefaultBranch, skillInInnerDefault,
            skillInDefaultBranch, rootSkill1, rootSkill2
        };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert — totals
        result.IncludedNodes.Should().HaveCount(8,
            "8 nodes should be included: rfg, Default Branch, Another Router, Branch 1 Branch, skill-in-branch1, skill-in-default-branch, root-skill-1, root-skill-2");
        result.ExcludedNodes.Should().HaveCount(4,
            "4 nodes should be excluded: OK Branch, skill-in-ok-branch, Inner Default Branch, skill-in-inner-default");

        // Assert — outer router filtering (ManuallySelectedBranch="Default")
        result.IncludedNodes.Should().Contain(outerRouter, "outer router itself is always included");
        result.IncludedNodes.Should().Contain(defaultBranch, "selected outer branch 'Default' is included");
        result.ExcludedNodes.Should().Contain(okBranch, "non-selected outer branch 'OK' is excluded");
        result.ExcludedNodes.Should().Contain(skillInOkBranch, "skill descendant of excluded 'OK' branch is excluded");

        // Assert — inner router filtering (ManuallySelectedBranch="Branch 1")
        result.IncludedNodes.Should().Contain(innerRouter, "inner router is descendant of selected outer branch");
        result.IncludedNodes.Should().Contain(branch1Branch, "selected inner branch 'Branch 1' is included");
        result.IncludedNodes.Should().Contain(skillInBranch1, "skill in selected inner branch is included");
        result.IncludedNodes.Should().NotContain(innerDefaultBranch, "non-selected inner branch must be excluded");
        result.ExcludedNodes.Should()
            .Contain(innerDefaultBranch, "non-selected inner branch is excluded by inner router");
        result.IncludedNodes.Should()
            .NotContain(skillInInnerDefault, "skill in non-selected inner branch must be excluded");
        result.ExcludedNodes.Should()
            .Contain(skillInInnerDefault, "skill descendant of excluded inner branch is excluded");

        // Assert — other included nodes
        result.IncludedNodes.Should().Contain(skillInDefaultBranch, "skill directly in Default Branch is included");
        result.IncludedNodes.Should().Contain(rootSkill1, "root orphan skill 1 is included");
        result.IncludedNodes.Should().Contain(rootSkill2, "root orphan skill 2 is included");

        // Assert — router selections recorded for both routers
        result.RouterSelections.Should().HaveCount(2);
        result.RouterSelections[outerRouterId].SelectedBranchName.Should().Be("Default");
        result.RouterSelections[outerRouterId].Reason.Should().Be("Manual selection");
        result.RouterSelections[innerRouterId].SelectedBranchName.Should().Be("Branch 1");
        result.RouterSelections[innerRouterId].Reason.Should().Be("Manual selection");
    }

    #endregion

    #region Phase 3: Nested Router Tests

    [Fact]
    public async Task FilterNodesAsync_NestedRouterBothWithSelection_ExcludesInnerNonSelectedBranch()
    {
        // Arrange
        // Hierarchy: OuterRouter → [BranchTask1 (selected), BranchTask2]
        //            BranchTask1 → InnerRouter → [InnerBranch1 (selected), InnerBranch2]
        //
        // Bug scenario: OuterRouter selects BranchTask1, so CollectAllDescendants adds
        // InnerRouter, InnerBranch1, AND InnerBranch2 to includedNodeIds.
        // Then InnerRouter processes and adds InnerBranch2 to excludedNodeIds.
        // Without the fix, InnerBranch2 stays in IncludedNodes because only includedNodeIds was checked.
        // With the fix, exclusion takes priority: InnerBranch2 must NOT appear in IncludedNodes.
        var procedureId = Guid.NewGuid();

        var outerRouterId = Guid.NewGuid();
        var outerBranch1Id = Guid.NewGuid();
        var outerBranch2Id = Guid.NewGuid();

        var innerRouterId = Guid.NewGuid();
        var innerBranch1Id = Guid.NewGuid();
        var innerBranch2Id = Guid.NewGuid();

        var innerBranch2ChildId = Guid.NewGuid();

        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Outer Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "outer" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Default", TargetNodeId = outerBranch1Id },
                    new() { Name = "Alternative", TargetNodeId = outerBranch2Id }
                },
                SelectedBranchTargetNodeId = outerBranch1Id,
                SelectedBranchName = "Default"
            }
        };

        var outerBranch1 = new TaskNode
        {
            Id = outerBranch1Id,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Default Branch Task", StartTime = 0, Duration = 100 }
        };

        var outerBranch2 = new TaskNode
        {
            Id = outerBranch2Id,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Alternative Branch Task", StartTime = 0, Duration = 50 }
        };

        var innerRouter = new RouterNode
        {
            Id = innerRouterId,
            ParentId = outerBranch1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Inner Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "inner" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch 1", TargetNodeId = innerBranch1Id },
                    new() { Name = "Branch 2", TargetNodeId = innerBranch2Id }
                },
                ManuallySelectedBranch = "Branch 1"
            }
        };

        var innerBranch1 = new TaskNode
        {
            Id = innerBranch1Id,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner Branch 1 Task", StartTime = 0, Duration = 30 }
        };

        var innerBranch2 = new TaskNode
        {
            Id = innerBranch2Id,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner Branch 2 Task", StartTime = 0, Duration = 40 }
        };

        var innerBranch2Child = new TaskNode
        {
            Id = innerBranch2ChildId,
            ParentId = innerBranch2Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner Branch 2 Child Task", StartTime = 0, Duration = 20 }
        };

        var nodes = new List<Node>
        {
            outerRouter, outerBranch1, outerBranch2,
            innerRouter, innerBranch1, innerBranch2, innerBranch2Child
        };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert — Outer router: Default branch selected
        result.IncludedNodes.Should().Contain(outerRouter, "outer router itself is always included");
        result.IncludedNodes.Should().Contain(outerBranch1, "selected outer branch is included");
        result.ExcludedNodes.Should().Contain(outerBranch2, "non-selected outer branch is excluded");
        result.IncludedNodes.Should().NotContain(outerBranch2);

        // Assert — Inner router: Branch 1 selected
        result.IncludedNodes.Should().Contain(innerRouter, "inner router is descendant of selected outer branch");
        result.IncludedNodes.Should().Contain(innerBranch1, "selected inner branch is included");

        // Assert — This is the critical fix: inner non-selected branch must be EXCLUDED, not included
        result.IncludedNodes.Should().NotContain(innerBranch2,
            "inner non-selected branch must not appear in IncludedNodes even though outer CollectAllDescendants added it");
        result.ExcludedNodes.Should().Contain(innerBranch2, "inner non-selected branch is excluded by inner router");

        // Assert — Descendants of inner non-selected branch must also be excluded
        result.IncludedNodes.Should().NotContain(innerBranch2Child,
            "descendant of inner non-selected branch must not appear in IncludedNodes");
        result.ExcludedNodes.Should().Contain(innerBranch2Child,
            "descendant of inner non-selected branch is excluded");

        // Assert — Router selections recorded for both routers
        result.RouterSelections.Should().HaveCount(2);
        result.RouterSelections[outerRouterId].SelectedBranchName.Should().Be("Default");
        result.RouterSelections[outerRouterId].Reason.Should().Be("Execution state");
        result.RouterSelections[innerRouterId].SelectedBranchName.Should().Be("Branch 1");
        result.RouterSelections[innerRouterId].Reason.Should().Be("Manual selection");
    }

    [Fact]
    public async Task FilterNodesAsync_NestedRouterOuterExcluded_InnerRouterBranchesAlsoExcluded()
    {
        // Arrange
        // Hierarchy: OuterRouter → [BranchTask1, BranchTask2 (selected)]
        //            BranchTask1 → InnerRouter → [InnerBranch1, InnerBranch2]
        //
        // When the outer router's non-selected branch contains a nested router,
        // the entire subtree (inner router + all its branches) must be excluded.
        var procedureId = Guid.NewGuid();

        var outerRouterId = Guid.NewGuid();
        var outerBranch1Id = Guid.NewGuid();
        var outerBranch2Id = Guid.NewGuid();

        var innerRouterId = Guid.NewGuid();
        var innerBranch1Id = Guid.NewGuid();
        var innerBranch2Id = Guid.NewGuid();

        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Outer Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "outer" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "WithNested", TargetNodeId = outerBranch1Id },
                    new() { Name = "Simple", TargetNodeId = outerBranch2Id }
                },
                SelectedBranchTargetNodeId = outerBranch2Id,
                SelectedBranchName = "Simple"
            }
        };

        var outerBranch1 = new TaskNode
        {
            Id = outerBranch1Id,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "WithNested Branch", StartTime = 0, Duration = 100 }
        };

        var outerBranch2 = new TaskNode
        {
            Id = outerBranch2Id,
            ParentId = outerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Simple Branch", StartTime = 0, Duration = 50 }
        };

        var innerRouter = new RouterNode
        {
            Id = innerRouterId,
            ParentId = outerBranch1Id,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Inner Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "inner" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "A", TargetNodeId = innerBranch1Id },
                    new() { Name = "B", TargetNodeId = innerBranch2Id }
                }
                // No selection — design-time
            }
        };

        var innerBranch1 = new TaskNode
        {
            Id = innerBranch1Id,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner A", StartTime = 0, Duration = 30 }
        };

        var innerBranch2 = new TaskNode
        {
            Id = innerBranch2Id,
            ParentId = innerRouterId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Inner B", StartTime = 0, Duration = 40 }
        };

        var nodes = new List<Node>
        {
            outerRouter, outerBranch1, outerBranch2,
            innerRouter, innerBranch1, innerBranch2
        };
        var service = new RouterBranchFilterService(_logger);

        // Act
        var result = await service.FilterNodesAsync(nodes);

        // Assert — Outer router selects Simple, so WithNested subtree is fully excluded
        result.IncludedNodes.Should().Contain(outerRouter);
        result.IncludedNodes.Should().Contain(outerBranch2, "selected outer branch is included");
        result.ExcludedNodes.Should().Contain(outerBranch1, "non-selected outer branch is excluded");

        // Assert — Entire inner router subtree is excluded as descendant of non-selected outer branch
        result.ExcludedNodes.Should().Contain(innerRouter, "inner router is descendant of excluded outer branch");
        result.ExcludedNodes.Should().Contain(innerBranch1, "inner branch A is descendant of excluded outer branch");
        result.ExcludedNodes.Should().Contain(innerBranch2, "inner branch B is descendant of excluded outer branch");

        result.IncludedNodes.Should().NotContain(innerRouter);
        result.IncludedNodes.Should().NotContain(innerBranch1);
        result.IncludedNodes.Should().NotContain(innerBranch2);

        // Only the outer router has a selection recorded
        result.RouterSelections.Should().HaveCount(1);
        result.RouterSelections[outerRouterId].SelectedBranchName.Should().Be("Simple");
    }

    #endregion
}