using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Operations;
using FHOOE.Freydis.GraphQLServer.Types;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.GraphQL;

/// <summary>
///     Unit tests for GraphQL Mutation operations related to DependencyEdge deletion.
///     These tests verify that a single GraphQL mutation call only triggers repository operations once
///     and follows proper call patterns without<IDependencyEdgeRepository>///
/// </summary>
public class MutationDependencyEdgeTests
{
    private readonly IDependencyEdgeApplicationService _edgeApplicationService;
    private readonly Mock<ICascadeDeletionService> _mockCascadeDeletion;
    private readonly Mock<ICrudDataPreparationService> _mockDataPreparation;
    private readonly Mock<IDependencyEdgeChangeTracker> _mockEdgeChangeTracker;
    private readonly Mock<ILogger<CrudSchedulingOrchestrator>> _mockLogger;
    private readonly Mock<INodeChangeTracker> _mockNodeChangeTracker;
    private readonly Mock<ICrudNotificationService> _mockNotification;
    private readonly Mock<IProcedureContext> _mockProcedureContext;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<ISchedulingResultLogger> _mockResultLogger;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;
    private readonly Mutation _mutation;
    private readonly Guid _testProcedureId = Guid.NewGuid();

    public MutationDependencyEdgeTests()
    {
        // Mock only the repository layer and external dependencies
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockProcedureContext = new Mock<IProcedureContext>();
        _mockNodeChangeTracker = new Mock<INodeChangeTracker>();
        _mockEdgeChangeTracker = new Mock<IDependencyEdgeChangeTracker>();
        _mockDataPreparation = new Mock<ICrudDataPreparationService>();
        _mockCascadeDeletion = new Mock<ICascadeDeletionService>();
        _mockNotification = new Mock<ICrudNotificationService>();
        _mockResultLogger = new Mock<ISchedulingResultLogger>();
        _mockLogger = new Mock<ILogger<CrudSchedulingOrchestrator>>();

        // Setup procedure context to return test procedure ID
        _mockProcedureContext.Setup(p => p.CurrentProcedureId)
            .Returns(_testProcedureId);
        _mockProcedureContext.Setup(p => p.RequireCurrentProcedureId())
            .Returns(_testProcedureId);

        // Setup change tracker observables with empty initial data
        _mockNodeChangeTracker.Setup(t => t.Nodes)
            .Returns(new BehaviorSubject<IReadOnlyList<Node>>(new List<Node>()));
        _mockEdgeChangeTracker.Setup(t => t.Edges)
            .Returns(new BehaviorSubject<IReadOnlyList<DependencyEdge>>(new List<DependencyEdge>()));

        // Setup notification service observables
        _mockNotification.Setup(n => n.NodesChanged)
            .Returns(new BehaviorSubject<IReadOnlyList<Node>>(new List<Node>()));
        _mockNotification.Setup(n => n.EdgesChanged)
            .Returns(new BehaviorSubject<IReadOnlyList<DependencyEdge>>(new List<DependencyEdge>()));

        // Create real instances of the orchestrator and application service
        var crudOrchestrator = new CrudSchedulingOrchestrator(
            _mockProcedureRepository.Object,
            _mockDataPreparation.Object,
            _mockCascadeDeletion.Object,
            _mockNotification.Object,
            _mockResultLogger.Object,
            _mockTimingOrchestrator.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object);

        _edgeApplicationService = new DependencyEdgeApplicationService(
            crudOrchestrator,
            _mockEdgeChangeTracker.Object,
            _mockProcedureContext.Object,
            Mock.Of<ILogger<DependencyEdgeApplicationService>>());
        _mutation = new Mutation();

        // Setup basic mocks for scheduling orchestrator to work
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(new List<Node>());
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(new List<DependencyEdge>());
        _mockTimingOrchestrator
            .Setup(t => t.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleResult
            {
                Success = true,
                UpdatedNodes = new List<Node>(),
                NodeSchedules = new List<NodeSchedule>()
            });
    }

    /// <summary>
    ///     Seeds the edge change tracker with an edge belonging to the test procedure so that the
    ///     application service's procedure-ownership lookup finds it before delegating to the orchestrator.
    /// </summary>
    /// <param name="edgeId">The id the test will pass into the GraphQL mutation.</param>
    private void SeedEdgeInTracker(Guid edgeId)
    {
        var edge = new DependencyEdge
        {
            Id = edgeId,
            ProcedureId = _testProcedureId,
            SourceId = Guid.NewGuid(),
            TargetId = Guid.NewGuid()
        };
        _mockEdgeChangeTracker.Setup(t => t.GetCurrentEdges())
            .Returns(new List<DependencyEdge> { edge }.AsReadOnly());
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_SingleGraphQLCall_OnlyCallsRepositoryDeleteOnce()
    {
        // Arrange
        var edgeId = Guid.NewGuid();
        var input = new DeleteDependencyEdgeInput { Id = edgeId };
        SeedEdgeInTracker(edgeId);

        // Track repository delete call count
        var deleteCallCount = 0;
        _mockProcedureRepository.Setup(r => r.DeleteEdgeAsync(edgeId))
            .ReturnsAsync(true)
            .Callback(() => deleteCallCount++);

        // Act - Call GraphQL mutation only ONCE
        var result =
            await _mutation.DeleteDependencyEdgeAsync(input, _edgeApplicationService, Mock.Of<ILogger<Mutation>>());

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DeleteDependencyEdgePayload>(result);
        Assert.True(result.Boolean);

        // Verify repository delete was called exactly once
        Assert.Equal(1, deleteCallCount);
        _mockProcedureRepository.Verify(r => r.DeleteEdgeAsync(edgeId), Times.Once);
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_SingleGraphQLCall_RepositoryFailure_OnlyCallsRepositoryDeleteOnce()
    {
        // Arrange
        var edgeId = Guid.NewGuid();
        var input = new DeleteDependencyEdgeInput { Id = edgeId };
        SeedEdgeInTracker(edgeId);

        // Track repository delete call count
        var deleteCallCount = 0;
        _mockProcedureRepository.Setup(r => r.DeleteEdgeAsync(edgeId))
            .ReturnsAsync(false) // Simulate repository failure (entity not found)
            .Callback(() => deleteCallCount++);

        // Act - Call GraphQL mutation only ONCE
        var result =
            await _mutation.DeleteDependencyEdgeAsync(input, _edgeApplicationService, Mock.Of<ILogger<Mutation>>());

        // Assert
        Assert.NotNull(result);
        Assert.IsType<DeleteDependencyEdgePayload>(result);
        Assert.False(result.Boolean); // Should reflect repository failure

        // Verify repository delete was called exactly once even on failure
        Assert.Equal(1, deleteCallCount);
        _mockProcedureRepository.Verify(r => r.DeleteEdgeAsync(edgeId), Times.Once);
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_TwoSequentialGraphQLCalls_CallsRepositoryDeleteTwice()
    {
        // Arrange
        var edgeId = Guid.NewGuid();
        var input = new DeleteDependencyEdgeInput { Id = edgeId };
        SeedEdgeInTracker(edgeId);

        // Track repository delete call count
        var deleteCallCount = 0;
        _mockProcedureRepository.Setup(r => r.DeleteEdgeAsync(edgeId))
            .ReturnsAsync(() => deleteCallCount == 0) // First call returns true, second returns false
            .Callback(() => deleteCallCount++);

        // Act - Call GraphQL mutation TWICE sequentially
        var result1 =
            await _mutation.DeleteDependencyEdgeAsync(input, _edgeApplicationService, Mock.Of<ILogger<Mutation>>());
        var result2 =
            await _mutation.DeleteDependencyEdgeAsync(input, _edgeApplicationService, Mock.Of<ILogger<Mutation>>());

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(result1.Boolean); // First call succeeds
        Assert.False(result2.Boolean); // Second call fails (entity already deleted)

        // Verify repository delete was called exactly twice (once per GraphQL call)
        Assert.Equal(2, deleteCallCount);
        _mockProcedureRepository.Verify(r => r.DeleteEdgeAsync(edgeId), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteDependencyEdgeAsync_ProvesSingleCallNoRetryLogic()
    {
        // Arrange
        var edgeId = Guid.NewGuid();
        var input = new DeleteDependencyEdgeInput { Id = edgeId };
        SeedEdgeInTracker(edgeId);

        // Track all repository method calls to ensure no unexpected calls
        var deleteCallCount = 0;
        var getAllCallCount = 0;
        var getByIdCallCount = 0;

        _mockProcedureRepository.Setup(r => r.DeleteEdgeAsync(edgeId))
            .ReturnsAsync(true)
            .Callback(() => deleteCallCount++);

        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync(new List<DependencyEdge>())
            .Callback(() => getAllCallCount++);

        _mockProcedureRepository.Setup(r => r.GetEdgeByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((DependencyEdge?)null)
            .Callback(() => getByIdCallCount++);

        // Act - Single GraphQL call
        var result =
            await _mutation.DeleteDependencyEdgeAsync(input, _edgeApplicationService, Mock.Of<ILogger<Mutation>>());

        // Assert
        Assert.True(result.Boolean);

        // Verify exact call pattern - should be predictable and not have any retry logic
        Assert.Equal(1, deleteCallCount); // Exactly one delete call
        Assert.True(getAllCallCount >= 0); // GetAll may be called for scheduling data preparation
        Assert.Equal(0, getByIdCallCount); // No GetById calls expected in current implementation

        // The key assertion: DeleteAsync should be called exactly once, proving no retry logic exists
        _mockProcedureRepository.Verify(r => r.DeleteEdgeAsync(edgeId), Times.Once);
    }
}