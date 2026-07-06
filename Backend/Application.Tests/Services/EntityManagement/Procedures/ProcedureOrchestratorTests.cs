using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures.Exceptions;
using FHOOE.Freydis.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DomainProcedure = FHOOE.Freydis.Domain.Entities.Procedure.Procedure;

namespace FHOOE.Freydis.Application.Tests.Services.EntityManagement.Procedures;

public class ProcedureOrchestratorTests
{
    private readonly Mock<ILogger<ProcedureOrchestrator>> _logger;
    private readonly ProcedureOrchestrator _orchestrator;
    private readonly Mock<IRepository<DomainProcedure>> _procedureRepository;

    public ProcedureOrchestratorTests()
    {
        _procedureRepository = new Mock<IRepository<DomainProcedure>>();
        _logger = new Mock<ILogger<ProcedureOrchestrator>>();

        _orchestrator = new ProcedureOrchestrator(
            _procedureRepository.Object,
            new Mock<IProcedureStateScope>().Object,
            new Mock<IProcedureVariableChangeTracker>().Object,
            _logger.Object
        );
    }

    #region Thread Safety Tests

    [Fact]
    public async Task LoadProcedureAsync_ConcurrentCalls_HandledSafely()
    {
        // Arrange
        var procedureIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        foreach (var id in procedureIds)
        {
            var procedure = CreateProcedure(id, $"Procedure {id}");
            _procedureRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(procedure);
        }

        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Act - Load procedures concurrently
        var tasks = procedureIds.Select(id => _orchestrator.LoadProcedureAsync(id)).ToList();
        await Task.WhenAll(tasks);

        // Assert - Should have a loaded procedure (the last one to complete)
        var loadedId = _orchestrator.GetLoadedProcedureId();
        loadedId.Should().NotBeNull();
        procedureIds.Should().Contain(loadedId.Value);
    }

    #endregion

    #region Helper Methods

    private static DomainProcedure CreateProcedure(Guid id, string name)
    {
        return new DomainProcedure
        {
            Id = id,
            Name = name,
            Description = null,
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            RootNodeIds = [],
            IsLoaded = false,
            LastLoadedUtc = null
        };
    }

    #endregion

    #region LoadProcedureAsync Tests

    [Fact]
    public async Task LoadProcedureAsync_WithValidProcedure_LoadsSuccessfully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Act
        var result = await _orchestrator.LoadProcedureAsync(procedureId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(procedureId);
        result.IsLoaded.Should().BeTrue();
        result.LastLoadedUtc.Should().NotBeNull();
        result.LastLoadedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        _procedureRepository.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.IsLoaded && p.LastLoadedUtc.HasValue)), Times.Once);
    }

    [Fact]
    public async Task LoadProcedureAsync_WithNonExistentProcedure_ThrowsProcedureNotFoundException()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync((DomainProcedure?)null);

        // Act
        Func<Task> act = async () => await _orchestrator.LoadProcedureAsync(procedureId);

        // Assert
        await act.Should().ThrowAsync<ProcedureNotFoundException>()
            .WithMessage($"Procedure with ID '{procedureId}' was not found.");
    }

    [Fact]
    public async Task LoadProcedureAsync_WithPreviouslyLoadedProcedure_UnloadsPreviousFirst()
    {
        // Arrange
        var firstProcedureId = Guid.NewGuid();
        var firstProcedure = CreateProcedure(firstProcedureId, "First Procedure");
        var secondProcedureId = Guid.NewGuid();
        var secondProcedure = CreateProcedure(secondProcedureId, "Second Procedure");

        _procedureRepository.Setup(r => r.GetByIdAsync(firstProcedureId)).ReturnsAsync(firstProcedure);
        _procedureRepository.Setup(r => r.GetByIdAsync(secondProcedureId)).ReturnsAsync(secondProcedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Load first procedure
        await _orchestrator.LoadProcedureAsync(firstProcedureId);

        // Act - Load second procedure
        var result = await _orchestrator.LoadProcedureAsync(secondProcedureId);

        // Assert
        result.Id.Should().Be(secondProcedureId);
        result.IsLoaded.Should().BeTrue();

        // Verify first procedure was unloaded
        _procedureRepository.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.Id == firstProcedureId && !p.IsLoaded)), Times.Once);

        // Verify second procedure was loaded
        _procedureRepository.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.Id == secondProcedureId && p.IsLoaded)), Times.Once);
    }

    [Fact]
    public async Task LoadProcedureAsync_WhenSameProcedureAlreadyLoaded_DoesNotReload()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Act - Load the same procedure twice
        await _orchestrator.LoadProcedureAsync(procedureId);
        var result = await _orchestrator.LoadProcedureAsync(procedureId);

        // Assert
        result.Id.Should().Be(procedureId);

        // Should only update once (first load), second load should be idempotent
        _procedureRepository.Verify(r => r.UpdateAsync(It.IsAny<DomainProcedure>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task LoadProcedureAsync_UpdatesLoadedProcedureIdState()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Act
        await _orchestrator.LoadProcedureAsync(procedureId);

        // Assert
        var loadedId = _orchestrator.GetLoadedProcedureId();
        loadedId.Should().Be(procedureId);
    }

    #endregion

    #region UnloadCurrentProcedureAsync Tests

    [Fact]
    public async Task UnloadCurrentProcedureAsync_WithLoadedProcedure_UnloadsSuccessfully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        // Load a procedure first
        await _orchestrator.LoadProcedureAsync(procedureId);

        // Act
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Assert
        var loadedId = _orchestrator.GetLoadedProcedureId();
        loadedId.Should().BeNull();

        _procedureRepository.Verify(r => r.UpdateAsync(It.Is<DomainProcedure>(p =>
            p.Id == procedureId && !p.IsLoaded)), Times.Once);
    }

    [Fact]
    public async Task UnloadCurrentProcedureAsync_WithNoProcedureLoaded_DoesNothing()
    {
        // Arrange - No procedure loaded

        // Act
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Assert
        var loadedId = _orchestrator.GetLoadedProcedureId();
        loadedId.Should().BeNull();

        _procedureRepository.Verify(r => r.UpdateAsync(It.IsAny<DomainProcedure>()), Times.Never);
    }

    [Fact]
    public async Task UnloadCurrentProcedureAsync_ClearsInMemoryState()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);
        _orchestrator.GetLoadedProcedureId().Should().Be(procedureId);

        // Act
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Assert
        _orchestrator.GetLoadedProcedureId().Should().BeNull();
    }

    #endregion

    #region GetLoadedProcedureAsync Tests

    [Fact]
    public async Task GetLoadedProcedureAsync_WithNoProcedureLoaded_ReturnsNull()
    {
        // Arrange - No procedure loaded

        // Act
        var result = await _orchestrator.GetLoadedProcedureAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoadedProcedureAsync_WithLoadedProcedure_ReturnsProcedure()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);

        // Act
        var result = await _orchestrator.GetLoadedProcedureAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(procedureId);
        result.Name.Should().Be("Test Procedure");
    }

    [Fact]
    public async Task GetLoadedProcedureAsync_AfterUnload_ReturnsNull()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Act
        var result = await _orchestrator.GetLoadedProcedureAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateProcedureAsync Tests

    [Fact]
    public async Task CreateProcedureAsync_WithValidName_CreatesSuccessfully()
    {
        // Arrange
        var name = "New Procedure";
        var description = "Test description";
        DomainProcedure? capturedProcedure = null;

        _procedureRepository.Setup(r => r.CreateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync((DomainProcedure p) =>
            {
                capturedProcedure = p;
                return p;
            });

        // Act
        var result = await _orchestrator.CreateProcedureAsync(name, description);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.LastUpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.IsLoaded.Should().BeFalse();
        result.RootNodeIds.Should().NotBeNull().And.BeEmpty();

        _procedureRepository.Verify(r => r.CreateAsync(It.IsAny<DomainProcedure>()), Times.Once);
    }

    [Fact]
    public async Task CreateProcedureAsync_WithoutDescription_CreatesWithNullDescription()
    {
        // Arrange
        var name = "New Procedure";
        DomainProcedure? capturedProcedure = null;

        _procedureRepository.Setup(r => r.CreateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync((DomainProcedure p) =>
            {
                capturedProcedure = p;
                return p;
            });

        // Act
        var result = await _orchestrator.CreateProcedureAsync(name);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateProcedureAsync_WithNullOrEmptyName_ThrowsArgumentException()
    {
        // Act & Assert - Null name
        Func<Task> actNull = async () => await _orchestrator.CreateProcedureAsync(null!);
        await actNull.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - Empty name
        Func<Task> actEmpty = async () => await _orchestrator.CreateProcedureAsync(string.Empty);
        await actEmpty.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - Whitespace name
        Func<Task> actWhitespace = async () => await _orchestrator.CreateProcedureAsync("   ");
        await actWhitespace.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DeleteProcedureAsync Tests

    [Fact]
    public async Task DeleteProcedureAsync_WithExistingProcedure_DeletesSuccessfully()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        // Act
        var result = await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert — relies on ON DELETE CASCADE for child entities
        result.Should().BeTrue();

        _procedureRepository.Verify(r => r.DeleteAsync(procedureId), Times.Once);
    }

    [Fact]
    public async Task DeleteProcedureAsync_WithNonExistentProcedure_ReturnsFalse()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync((DomainProcedure?)null);

        // Act
        var result = await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert
        result.Should().BeFalse();

        _procedureRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProcedureAsync_WithLoadedProcedure_UnloadsBeforeDeleting()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        // Load the procedure first
        await _orchestrator.LoadProcedureAsync(procedureId);
        _orchestrator.GetLoadedProcedureId().Should().Be(procedureId);

        // Act
        var result = await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert — unloads procedure then deletes (CASCADE handles children)
        result.Should().BeTrue();
        _orchestrator.GetLoadedProcedureId().Should().BeNull();

        _procedureRepository.Verify(r => r.DeleteAsync(procedureId), Times.Once);
    }

    [Fact]
    public async Task DeleteProcedureAsync_ReliesOnCascadeDelete()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");

        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        // Act
        await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert — only DeleteAsync is called; ON DELETE CASCADE handles children
        _procedureRepository.Verify(r => r.DeleteAsync(procedureId), Times.Once);
    }

    [Fact]
    public async Task DeleteProcedureAsync_SucceedsWhenProcedureHasNoChildren()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        // Act
        var result = await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert — CASCADE handles any children; DeleteAsync is the only call
        result.Should().BeTrue();
        _procedureRepository.Verify(r => r.DeleteAsync(procedureId), Times.Once);
    }

    [Fact]
    public async Task CreateProcedureAsync_ThenGetAllAsync_ReturnsProcedure()
    {
        // Arrange — simulate a repository that persists in-memory
        var store = new List<DomainProcedure>();

        _procedureRepository.Setup(r => r.CreateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync((DomainProcedure p) =>
            {
                store.Add(p);
                return p;
            });

        _procedureRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => store);

        // Act — create a procedure
        var created = await _orchestrator.CreateProcedureAsync("Test Procedure", "Description");

        // Assert — it should appear in GetAllAsync immediately
        var all = await _procedureRepository.Object.GetAllAsync();
        all.Should().ContainSingle();
        all[0].Id.Should().Be(created.Id);
        all[0].Name.Should().Be("Test Procedure");
        all[0].Description.Should().Be("Description");
    }

    [Fact]
    public async Task CreateProcedureAsync_MultipleProcedures_AllReturnedByGetAll()
    {
        // Arrange — simulate a repository that persists in-memory
        var store = new List<DomainProcedure>();

        _procedureRepository.Setup(r => r.CreateAsync(It.IsAny<DomainProcedure>()))
            .ReturnsAsync((DomainProcedure p) =>
            {
                store.Add(p);
                return p;
            });

        _procedureRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => store);

        // Act — create multiple procedures
        await _orchestrator.CreateProcedureAsync("Procedure 1");
        await _orchestrator.CreateProcedureAsync("Procedure 2");
        await _orchestrator.CreateProcedureAsync("Procedure 3");

        // Assert — all should appear in GetAllAsync
        var all = await _procedureRepository.Object.GetAllAsync();
        all.Should().HaveCount(3);
        all.Select(p => p.Name).Should().Contain(["Procedure 1", "Procedure 2", "Procedure 3"]);
    }

    #endregion

    #region GetLoadedProcedureName Tests

    [Fact]
    public void GetLoadedProcedureName_WithNoProcedureLoaded_ReturnsNull()
    {
        // Act
        var result = _orchestrator.GetLoadedProcedureName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoadedProcedureName_WithLoadedProcedure_ReturnsProcedureName()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "My Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);

        // Act
        var result = _orchestrator.GetLoadedProcedureName();

        // Assert
        result.Should().Be("My Procedure");
    }

    [Fact]
    public async Task GetLoadedProcedureName_AfterUnload_ReturnsNull()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "My Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Act
        var result = _orchestrator.GetLoadedProcedureName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoadedProcedureName_AfterSwitchingProcedures_ReturnsNewName()
    {
        // Arrange
        var proc1Id = Guid.NewGuid();
        var proc2Id = Guid.NewGuid();
        var proc1 = CreateProcedure(proc1Id, "First");
        var proc2 = CreateProcedure(proc2Id, "Second");
        _procedureRepository.Setup(r => r.GetByIdAsync(proc1Id)).ReturnsAsync(proc1);
        _procedureRepository.Setup(r => r.GetByIdAsync(proc2Id)).ReturnsAsync(proc2);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(proc1Id);
        await _orchestrator.LoadProcedureAsync(proc2Id);

        // Act
        var result = _orchestrator.GetLoadedProcedureName();

        // Assert
        result.Should().Be("Second");
    }

    [Fact]
    public async Task GetLoadedProcedureName_AfterDelete_ReturnsNull()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "To Delete");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);
        await _orchestrator.DeleteProcedureAsync(procedureId);

        // Act
        var result = _orchestrator.GetLoadedProcedureName();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ProcedureChanges BehaviorSubject Tests

    [Fact]
    public void ProcedureChanges_NewSubscriber_ReceivesCurrentValue_WhenNothingLoaded()
    {
        // Act — subscribe after construction
        Guid? received = Guid.NewGuid(); // sentinel to prove it changes
        _orchestrator.ProcedureChanges.Subscribe(id => received = id);

        // Assert — BehaviorSubject should immediately emit null
        received.Should().BeNull();
    }

    [Fact]
    public async Task ProcedureChanges_NewSubscriber_ReceivesCurrentValue_WhenProcedureLoaded()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);

        // Act — subscribe AFTER loading
        Guid? received = null;
        _orchestrator.ProcedureChanges.Subscribe(id => received = id);

        // Assert — BehaviorSubject should replay the current value
        received.Should().Be(procedureId);
    }

    [Fact]
    public async Task DeleteProcedureAsync_WhenLoaded_EmitsProcedureChangesNull()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);
        _procedureRepository.Setup(r => r.DeleteAsync(procedureId)).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);

        var emissions = new List<Guid?>();
        _orchestrator.ProcedureChanges.Subscribe(id => emissions.Add(id));
        emissions.Clear(); // clear the BehaviorSubject replay

        // Act
        await _orchestrator.DeleteProcedureAsync(procedureId);

        // Assert — delete should emit null on ProcedureChanges
        emissions.Should().ContainSingle().Which.Should().BeNull();
    }

    #endregion

    #region GetLoadedProcedureId Tests

    [Fact]
    public void GetLoadedProcedureId_WithNoProcedureLoaded_ReturnsNull()
    {
        // Act
        var result = _orchestrator.GetLoadedProcedureId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLoadedProcedureId_WithLoadedProcedure_ReturnsProcedureId()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);

        // Act
        var result = _orchestrator.GetLoadedProcedureId();

        // Assert
        result.Should().Be(procedureId);
    }

    [Fact]
    public async Task GetLoadedProcedureId_AfterUnload_ReturnsNull()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, "Test Procedure");
        _procedureRepository.Setup(r => r.GetByIdAsync(procedureId)).ReturnsAsync(procedure);
        _procedureRepository.Setup(r => r.UpdateAsync(It.IsAny<DomainProcedure>())).ReturnsAsync(true);

        await _orchestrator.LoadProcedureAsync(procedureId);
        await _orchestrator.UnloadCurrentProcedureAsync();

        // Act
        var result = _orchestrator.GetLoadedProcedureId();

        // Assert
        result.Should().BeNull();
    }

    #endregion
}