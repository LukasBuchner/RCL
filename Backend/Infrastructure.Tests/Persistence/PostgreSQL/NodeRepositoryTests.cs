using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using SystemTask = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.PostgreSQL;

/// <summary>
///     Tests for ProcedureRepository node operations against PostgreSQL.
///     Uses Testcontainers to spin up a real PostgreSQL instance.
/// </summary>
[Trait("Category", "Integration")]
public class NodeRepositoryTests : IClassFixture<PostgresTestFixture>, IAsyncLifetime
{
    private readonly List<Guid> _createdProcedureIds = [];
    private readonly PostgresTestFixture _fixture;
    private readonly ProcedureRepository _procedureRepository;

    public NodeRepositoryTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
        _procedureRepository = new ProcedureRepository(fixture.Context);
    }

    public SystemTask InitializeAsync()
    {
        return SystemTask.CompletedTask;
    }

    public async SystemTask DisposeAsync()
    {
        // Cleanup: Delete procedures (nodes cascade-delete via FK)
        foreach (var id in _createdProcedureIds)
            await _procedureRepository.DeleteAsync(id);
    }

    /// <summary>
    ///     Creates a minimal procedure in the database to satisfy the FK constraint on nodes.
    /// </summary>
    private async Task<Guid> CreateProcedureAsync()
    {
        var procedureId = Guid.NewGuid();
        var procedure = new Procedure
        {
            Id = procedureId,
            Name = $"Test Procedure {procedureId}",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            Variables = new List<VariableDefinition>()
        };
        await _procedureRepository.CreateAsync(procedure);
        _createdProcedureIds.Add(procedureId);
        return procedureId;
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_ReturnsNodes_WhenNodesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var node1 = CreateTestSkillExecutionNode(procedureId);
        var node2 = CreateTestSkillExecutionNode(procedureId);
        var nodeFromOtherProcedure = CreateTestSkillExecutionNode(otherProcedureId);

        await _procedureRepository.CreateNodeAsync(node1);
        await _procedureRepository.CreateNodeAsync(node2);
        await _procedureRepository.CreateNodeAsync(nodeFromOtherProcedure);

        // Act
        var result = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.ProcedureId.Should().Be(procedureId));
        result.Select(n => n.Id).Should().Contain([node1.Id, node2.Id]);
        result.Select(n => n.Id).Should().NotContain(nodeFromOtherProcedure.Id);
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_ReturnsEmptyList_WhenNoNodesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var nodeFromOtherProcedure = CreateTestSkillExecutionNode(otherProcedureId);
        await _procedureRepository.CreateNodeAsync(nodeFromOtherProcedure);

        // Act
        var result = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_ReturnsOnlyNodesForSpecificProcedure()
    {
        // Arrange
        var procedure1Id = await CreateProcedureAsync();
        var procedure2Id = await CreateProcedureAsync();
        var procedure3Id = await CreateProcedureAsync();

        await _procedureRepository.CreateNodeAsync(CreateTestSkillExecutionNode(procedure1Id));
        await _procedureRepository.CreateNodeAsync(CreateTestSkillExecutionNode(procedure1Id));
        await _procedureRepository.CreateNodeAsync(CreateTestSkillExecutionNode(procedure2Id));
        await _procedureRepository.CreateNodeAsync(CreateTestSkillExecutionNode(procedure3Id));

        // Act
        var resultProc1 = await _procedureRepository.GetNodesByProcedureIdAsync(procedure1Id);
        var resultProc2 = await _procedureRepository.GetNodesByProcedureIdAsync(procedure2Id);
        var resultProc3 = await _procedureRepository.GetNodesByProcedureIdAsync(procedure3Id);

        // Assert
        resultProc1.Should().HaveCount(2);
        resultProc2.Should().HaveCount(1);
        resultProc3.Should().HaveCount(1);

        resultProc1.Should().AllSatisfy(n => n.ProcedureId.Should().Be(procedure1Id));
        resultProc2.Should().AllSatisfy(n => n.ProcedureId.Should().Be(procedure2Id));
        resultProc3.Should().AllSatisfy(n => n.ProcedureId.Should().Be(procedure3Id));
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_DeletesAllNodesForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var node1 = CreateTestSkillExecutionNode(procedureId);
        var node2 = CreateTestSkillExecutionNode(procedureId);
        var nodeToKeep = CreateTestSkillExecutionNode(otherProcedureId);

        await _procedureRepository.CreateNodeAsync(node1);
        await _procedureRepository.CreateNodeAsync(node2);
        await _procedureRepository.CreateNodeAsync(nodeToKeep);

        // Act
        var result = await _procedureRepository.DeleteNodesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeTrue();

        // Verify nodes were deleted
        var deletedNodes = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        deletedNodes.Should().BeEmpty();

        // Verify other procedure's nodes were not affected
        var remainingNode = await _procedureRepository.GetNodeByIdAsync(nodeToKeep.Id);
        remainingNode.Should().NotBeNull();
        remainingNode!.ProcedureId.Should().Be(otherProcedureId);
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_ReturnsFalse_WhenNoNodesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        // Create a node in a different procedure
        await _procedureRepository.CreateNodeAsync(CreateTestSkillExecutionNode(otherProcedureId));

        // Act
        var result = await _procedureRepository.DeleteNodesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeFalse();
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_HandlesMultipleNodeTypes()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();

        var skillNode = CreateTestSkillExecutionNode(procedureId);
        var taskNode = CreateTestTaskNode(procedureId);
        var routerNode = CreateTestRouterNode(procedureId);

        await _procedureRepository.CreateNodeAsync(skillNode);
        await _procedureRepository.CreateNodeAsync(taskNode);
        await _procedureRepository.CreateNodeAsync(routerNode);

        // Act
        var result = await _procedureRepository.DeleteNodesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeTrue();

        // Verify all node types were deleted
        var remainingNodes = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        remainingNodes.Should().BeEmpty();
    }

    private static SkillExecutionNode CreateTestSkillExecutionNode(Guid procedureId)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill Task",
                StartTime = 0,
                Duration = 1.0,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Skill",
                    Description = "Test Description",
                    Properties = []
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static TaskNode CreateTestTaskNode(Guid procedureId)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task
            {
                Name = "Test Task",
                StartTime = 0,
                Duration = 1.0
            }
        };
    }

    private static RouterNode CreateTestRouterNode(Guid procedureId)
    {
        return new RouterNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Test Router",
                StartTime = 0,
                Duration = 1.0,
                Selector = new SimpleVariableSelector
                {
                    Expression = "testVar"
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "Test Branch",
                        Condition = "true",
                        TargetNodeId = Guid.NewGuid()
                    }
                }
            }
        };
    }
}