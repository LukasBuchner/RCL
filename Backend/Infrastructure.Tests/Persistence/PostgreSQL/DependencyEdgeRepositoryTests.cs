using SystemTask = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.PostgreSQL;

/// <summary>
///     Tests for ProcedureRepository edge operations against PostgreSQL.
///     Uses Testcontainers to spin up a real PostgreSQL instance.
/// </summary>
[Trait("Category", "Integration")]
public class DependencyEdgeRepositoryTests : IClassFixture<PostgresTestFixture>, IAsyncLifetime
{
    private readonly List<Guid> _createdProcedureIds = [];
    private readonly PostgresTestFixture _fixture;
    private readonly ProcedureRepository _procedureRepository;

    public DependencyEdgeRepositoryTests(PostgresTestFixture fixture)
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
    public async SystemTask GetByProcedureIdAsync_ReturnsEdges_WhenEdgesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var edge1 = CreateTestEdge(procedureId);
        var edge2 = CreateTestEdge(procedureId);
        var edgeFromOtherProcedure = CreateTestEdge(otherProcedureId);

        await _procedureRepository.CreateEdgeAsync(edge1);
        await _procedureRepository.CreateEdgeAsync(edge2);
        await _procedureRepository.CreateEdgeAsync(edgeFromOtherProcedure);

        // Act
        var result = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.ProcedureId.Should().Be(procedureId));
        result.Select(e => e.Id).Should().Contain([edge1.Id, edge2.Id]);
        result.Select(e => e.Id).Should().NotContain(edgeFromOtherProcedure.Id);
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_ReturnsEmptyList_WhenNoEdgesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var edgeFromOtherProcedure = CreateTestEdge(otherProcedureId);
        await _procedureRepository.CreateEdgeAsync(edgeFromOtherProcedure);

        // Act
        var result = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_ReturnsOnlyEdgesForSpecificProcedure()
    {
        // Arrange
        var procedure1Id = await CreateProcedureAsync();
        var procedure2Id = await CreateProcedureAsync();
        var procedure3Id = await CreateProcedureAsync();

        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(procedure1Id));
        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(procedure1Id));
        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(procedure1Id));
        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(procedure2Id));
        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(procedure3Id));

        // Act
        var resultProc1 = await _procedureRepository.GetEdgesByProcedureIdAsync(procedure1Id);
        var resultProc2 = await _procedureRepository.GetEdgesByProcedureIdAsync(procedure2Id);
        var resultProc3 = await _procedureRepository.GetEdgesByProcedureIdAsync(procedure3Id);

        // Assert
        resultProc1.Should().HaveCount(3);
        resultProc2.Should().HaveCount(1);
        resultProc3.Should().HaveCount(1);

        resultProc1.Should().AllSatisfy(e => e.ProcedureId.Should().Be(procedure1Id));
        resultProc2.Should().AllSatisfy(e => e.ProcedureId.Should().Be(procedure2Id));
        resultProc3.Should().AllSatisfy(e => e.ProcedureId.Should().Be(procedure3Id));
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_DeletesAllEdgesForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        var edge1 = CreateTestEdge(procedureId);
        var edge2 = CreateTestEdge(procedureId);
        var edgeToKeep = CreateTestEdge(otherProcedureId);

        await _procedureRepository.CreateEdgeAsync(edge1);
        await _procedureRepository.CreateEdgeAsync(edge2);
        await _procedureRepository.CreateEdgeAsync(edgeToKeep);

        // Act
        var result = await _procedureRepository.DeleteEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeTrue();

        // Verify edges were deleted
        var deletedEdges = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);
        deletedEdges.Should().BeEmpty();

        // Verify other procedure's edges were not affected
        var remainingEdge = await _procedureRepository.GetEdgeByIdAsync(edgeToKeep.Id);
        remainingEdge.Should().NotBeNull();
        remainingEdge!.ProcedureId.Should().Be(otherProcedureId);
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_ReturnsFalse_WhenNoEdgesExistForProcedure()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var otherProcedureId = await CreateProcedureAsync();

        // Create an edge in a different procedure
        await _procedureRepository.CreateEdgeAsync(CreateTestEdge(otherProcedureId));

        // Act
        var result = await _procedureRepository.DeleteEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeFalse();
    }

    [IntegrationFact]
    public async SystemTask DeleteByProcedureIdAsync_HandlesMultipleEdges()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();

        // Create multiple edges with different configurations
        var edge1 = CreateTestEdge(procedureId);
        var edge2 = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid(),
            SourceHandle = "output",
            TargetHandle = "input"
        };
        var edge3 = CreateTestEdge(procedureId);

        await _procedureRepository.CreateEdgeAsync(edge1);
        await _procedureRepository.CreateEdgeAsync(edge2);
        await _procedureRepository.CreateEdgeAsync(edge3);

        // Act
        var result = await _procedureRepository.DeleteEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().BeTrue();

        // Verify all edges were deleted
        var remainingEdges = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);
        remainingEdges.Should().BeEmpty();
    }

    [IntegrationFact]
    public async SystemTask GetByProcedureIdAsync_PreservesEdgeProperties()
    {
        // Arrange
        var procedureId = await CreateProcedureAsync();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var edge = new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "output-1",
            TargetHandle = "input-2"
        };

        await _procedureRepository.CreateEdgeAsync(edge);

        // Act
        var result = await _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

        // Assert
        result.Should().HaveCount(1);
        var retrievedEdge = result.First();
        retrievedEdge.ProcedureId.Should().Be(procedureId);
        retrievedEdge.SourceId.Should().Be(sourceId);
        retrievedEdge.TargetId.Should().Be(targetId);
        retrievedEdge.SourceHandle.Should().Be("output-1");
        retrievedEdge.TargetHandle.Should().Be("input-2");
    }

    private static DependencyEdge CreateTestEdge(Guid procedureId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
    }
}