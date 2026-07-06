using SystemTask = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.PostgreSQL;

/// <summary>
///     Tests for CreateNodeAsync upsert behavior in ProcedureRepository against PostgreSQL.
///     Verifies that CreateNodeAsync uses ON CONFLICT DO UPDATE (upsert) semantics
///     to gracefully handle duplicate key scenarios.
/// </summary>
[Trait("Category", "Integration")]
public class CreateAsyncUpsertTests : IClassFixture<PostgresTestFixture>, IAsyncLifetime
{
    private readonly List<Guid> _createdProcedureIds = [];
    private readonly PostgresTestFixture _fixture;
    private readonly ProcedureRepository _procedureRepository;

    public CreateAsyncUpsertTests(PostgresTestFixture fixture)
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
        foreach (var id in _createdProcedureIds)
            await _procedureRepository.DeleteAsync(id);
    }

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
    public async SystemTask CreateAsync_WithSameId_ShouldUpsertSuccessfully()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var nodeId = Guid.NewGuid();

        var originalNode = CreateTestSkillExecutionNode(nodeId, procedureId, "Original Name");
        var updatedNode = CreateTestSkillExecutionNode(nodeId, procedureId, "Updated Name");

        // Act - Create the same node twice with the same ID
        var firstCreate = await _procedureRepository.CreateNodeAsync(originalNode);
        var secondCreate = await _procedureRepository.CreateNodeAsync(updatedNode);

        // Assert - Should not throw duplicate key error
        firstCreate.Should().NotBeNull();
        secondCreate.Should().NotBeNull();

        // Verify the node was upserted (replaced) with the updated data
        var retrievedNode = await _procedureRepository.GetNodeByIdAsync(nodeId);
        retrievedNode.Should().NotBeNull();
        retrievedNode.Should().BeOfType<SkillExecutionNode>();
        var skillNode = (SkillExecutionNode)retrievedNode!;
        skillNode.SkillExecutionTask.Name.Should().Be("Updated Name");
    }

    [IntegrationFact]
    public async SystemTask CreateAsync_WithSameId_MultipleTimesInParallel_ShouldNotFail()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var nodeId = Guid.NewGuid();

        var node1 = CreateTestSkillExecutionNode(nodeId, procedureId, "Version 1");
        var node2 = CreateTestSkillExecutionNode(nodeId, procedureId, "Version 2");
        var node3 = CreateTestSkillExecutionNode(nodeId, procedureId, "Version 3");

        // Act - Create the same node multiple times in parallel
        var createTasks = new[]
        {
            _procedureRepository.CreateNodeAsync(node1),
            _procedureRepository.CreateNodeAsync(node2),
            _procedureRepository.CreateNodeAsync(node3)
        };

        var results = await SystemTask.WhenAll(createTasks);

        // Assert - None should throw duplicate key error
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Verify a node with that ID exists
        var retrievedNode = await _procedureRepository.GetNodeByIdAsync(nodeId);
        retrievedNode.Should().NotBeNull();
        retrievedNode!.Id.Should().Be(nodeId);
    }

    [IntegrationFact]
    public async SystemTask CreateAsync_AfterDelete_ShouldRecreateSuccessfully()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var nodeId = Guid.NewGuid();

        var originalNode = CreateTestSkillExecutionNode(nodeId, procedureId, "Original");
        var recreatedNode = CreateTestSkillExecutionNode(nodeId, procedureId, "Recreated");

        // Act
        await _procedureRepository.CreateNodeAsync(originalNode);
        var deleteResult = await _procedureRepository.DeleteNodeAsync(nodeId);
        var recreateResult = await _procedureRepository.CreateNodeAsync(recreatedNode);

        // Assert
        deleteResult.Should().BeTrue();
        recreateResult.Should().NotBeNull();

        var retrievedNode = await _procedureRepository.GetNodeByIdAsync(nodeId);
        retrievedNode.Should().NotBeNull();
        var skillNode = (SkillExecutionNode)retrievedNode!;
        skillNode.SkillExecutionTask.Name.Should().Be("Recreated");
    }

    [IntegrationFact]
    public async SystemTask CreateAsync_WithDifferentIds_ShouldCreateMultipleNodes()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var node1 = CreateTestSkillExecutionNode(Guid.NewGuid(), procedureId, "Node 1");
        var node2 = CreateTestSkillExecutionNode(Guid.NewGuid(), procedureId, "Node 2");
        var node3 = CreateTestSkillExecutionNode(Guid.NewGuid(), procedureId, "Node 3");

        // Act
        await _procedureRepository.CreateNodeAsync(node1);
        await _procedureRepository.CreateNodeAsync(node2);
        await _procedureRepository.CreateNodeAsync(node3);

        // Assert - All nodes should exist
        var allNodes = await _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        allNodes.Should().HaveCount(3);
        allNodes.Select(n => n.Id).Should().Contain([node1.Id, node2.Id, node3.Id]);
    }

    [IntegrationFact]
    public async SystemTask CreateAsync_SimulatingSchedulingRaceCondition_ShouldHandleGracefully()
    {
        // Arrange - Simulates the scenario where:
        // 1. GraphQL mutation creates a node
        // 2. Scheduling orchestrator tries to create the same node in parallel
        var procedureId = await CreateProcedureAsync();
        var nodeId = Guid.NewGuid();

        var nodeFromGraphQl = CreateTestSkillExecutionNode(nodeId, procedureId, "From GraphQL");
        var nodeFromScheduling = CreateTestSkillExecutionNode(nodeId, procedureId, "From Scheduling");

        // Act - Simulate parallel creation from different sources
        var graphQlTask = _procedureRepository.CreateNodeAsync(nodeFromGraphQl);
        var schedulingTask = _procedureRepository.CreateNodeAsync(nodeFromScheduling);

        // Should not throw duplicate key error
        var results = await SystemTask.WhenAll(graphQlTask, schedulingTask);

        // Assert
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Verify exactly one node exists with that ID
        var retrievedNode = await _procedureRepository.GetNodeByIdAsync(nodeId);
        retrievedNode.Should().NotBeNull();
        retrievedNode!.Id.Should().Be(nodeId);
    }

    private static SkillExecutionNode CreateTestSkillExecutionNode(Guid nodeId, Guid procedureId, string taskName)
    {
        return new SkillExecutionNode
        {
            Id = nodeId,
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = taskName,
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
}