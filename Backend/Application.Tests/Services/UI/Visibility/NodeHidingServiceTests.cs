using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.UI.Visibility;

public class NodeHidingServiceTests
{
    private readonly ILogger<NodeHidingService> _logger;
    private readonly INodeHidingService _service;

    public NodeHidingServiceTests()
    {
        _logger = Mock.Of<ILogger<NodeHidingService>>();
        _service = new NodeHidingService(_logger);
    }

    [Fact]
    public async Task ApplyHiddenState_WithEmptyNodeList_ReturnsEmpty()
    {
        // Arrange
        var allNodes = Array.Empty<Node>();
        var nodesToHide = new List<Guid>();

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyHiddenState_WithNullNodeList_ReturnsEmpty()
    {
        // Arrange
        IReadOnlyList<Node> allNodes = null!;
        var nodesToHide = new List<Guid>();

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyHiddenState_WithEmptyHideList_AllNodesVisible()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1", true);
        var node2 = CreateTaskNode("Node2", true);
        var allNodes = new List<Node> { node1, node2 };
        var nodesToHide = new List<Guid>();

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.Hidden.Should().BeFalse());
    }

    [Fact]
    public async Task ApplyHiddenState_WithNodesToHide_MarksThemHidden()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1");
        var node2 = CreateTaskNode("Node2");
        var node3 = CreateTaskNode("Node3");
        var allNodes = new List<Node> { node1, node2, node3 };
        var nodesToHide = new List<Guid> { node2.Id, node3.Id };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(3);

        var resultNode1 = result.First(n => n.Id == node1.Id);
        resultNode1.Hidden.Should().BeFalse();

        var resultNode2 = result.First(n => n.Id == node2.Id);
        resultNode2.Hidden.Should().BeTrue();

        var resultNode3 = result.First(n => n.Id == node3.Id);
        resultNode3.Hidden.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyHiddenState_WithNodesToHide_OthersRemainVisible()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1");
        var node2 = CreateTaskNode("Node2");
        var node3 = CreateTaskNode("Node3");
        var allNodes = new List<Node> { node1, node2, node3 };
        var nodesToHide = new List<Guid> { node2.Id };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(3);

        var resultNode1 = result.First(n => n.Id == node1.Id);
        resultNode1.Hidden.Should().BeFalse();

        var resultNode2 = result.First(n => n.Id == node2.Id);
        resultNode2.Hidden.Should().BeTrue();

        var resultNode3 = result.First(n => n.Id == node3.Id);
        resultNode3.Hidden.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyHiddenState_WhenAlreadyHidden_NoChange()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1", true);
        var allNodes = new List<Node> { node1 };
        var nodesToHide = new List<Guid> { node1.Id };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(1);
        var resultNode = result.First();
        resultNode.Hidden.Should().BeTrue();
        resultNode.Should().BeSameAs(node1); // Should not create new instance if state unchanged
    }

    [Fact]
    public async Task ApplyHiddenState_WhenHiddenBecomesVisible_Updates()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1", true);
        var node2 = CreateTaskNode("Node2", true);
        var allNodes = new List<Node> { node1, node2 };
        var nodesToHide = new List<Guid> { node1.Id }; // Only node1 should remain hidden

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(2);

        var resultNode1 = result.First(n => n.Id == node1.Id);
        resultNode1.Hidden.Should().BeTrue();
        resultNode1.Should().BeSameAs(node1); // No change needed

        var resultNode2 = result.First(n => n.Id == node2.Id);
        resultNode2.Hidden.Should().BeFalse();
        resultNode2.Should().NotBeSameAs(node2); // New instance created
    }

    [Fact]
    public async Task ApplyHiddenState_WithNonExistentIds_IgnoresThem()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1");
        var node2 = CreateTaskNode("Node2");
        var allNodes = new List<Node> { node1, node2 };
        var nonExistentId = Guid.NewGuid();
        var nodesToHide = new List<Guid> { node1.Id, nonExistentId };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(2);

        var resultNode1 = result.First(n => n.Id == node1.Id);
        resultNode1.Hidden.Should().BeTrue();

        var resultNode2 = result.First(n => n.Id == node2.Id);
        resultNode2.Hidden.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyHiddenState_PreservesOtherNodeProperties()
    {
        // Arrange
        var node1 = CreateTaskNode("Node1");
        var allNodes = new List<Node> { node1 };
        var nodesToHide = new List<Guid> { node1.Id };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(1);
        var resultNode = result.First() as TaskNode;

        resultNode.Should().NotBeNull();
        resultNode!.Id.Should().Be(node1.Id);
        resultNode.ProcedureId.Should().Be(node1.ProcedureId);
        resultNode.Position.Should().Be(node1.Position);
        resultNode.Hidden.Should().BeTrue(); // Only Hidden should change
    }

    [Fact]
    public async Task ApplyHiddenState_WithMultipleNodeTypes_WorksForAll()
    {
        // Arrange
        var taskNode = CreateTaskNode("TaskNode");
        var routerNode = CreateRouterNode("RouterNode");
        var allNodes = new List<Node> { taskNode, routerNode };
        var nodesToHide = new List<Guid> { routerNode.Id };

        // Act
        var result = await _service.ApplyHiddenStateAsync(allNodes, nodesToHide);

        // Assert
        result.Should().HaveCount(2);

        var resultTaskNode = result.First(n => n.Id == taskNode.Id);
        resultTaskNode.Hidden.Should().BeFalse();
        resultTaskNode.Should().BeOfType<TaskNode>();

        var resultRouterNode = result.First(n => n.Id == routerNode.Id);
        resultRouterNode.Hidden.Should().BeTrue();
        resultRouterNode.Should().BeOfType<RouterNode>();
    }

    // Helper methods
    private static TaskNode CreateTaskNode(string name, bool hidden = false)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Hidden = hidden,
            Task = new DomainTask
            {
                Name = name,
                StartTime = 0,
                Duration = 10
            }
        };
    }

    private static RouterNode CreateRouterNode(string name, bool hidden = false)
    {
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Hidden = hidden,
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0,
                Duration = 10,
                Selector = new SimpleVariableSelector { Expression = "" },
                Branches = new List<ConditionalBranch>()
            }
        };
    }
}