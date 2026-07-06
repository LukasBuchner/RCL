using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;
using Node = FHOOE.Freydis.Domain.Entities.Procedure.Node;
using DependencyEdge = FHOOE.Freydis.Domain.Entities.Procedure.DependencyEdge;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Initialization;

/// <summary>
///     Tests for ExecutionInitializer variable context initialization functionality.
/// </summary>
public class ExecutionInitializerVariableContextTests
{
    private readonly ExecutionInitializer _initializer;
    private readonly Mock<IExecutionIdAssigner> _mockIdAssigner;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;
    private readonly Mock<IVariableResolver> _mockVariableResolver;

    public ExecutionInitializerVariableContextTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockIdAssigner = new Mock<IExecutionIdAssigner>();
        _mockVariableResolver = new Mock<IVariableResolver>();

        var mockAgentManager = new Mock<IAgentManager>();
        var mockProcedureContext = new Mock<IProcedureContext>();
        var mockLogger = new Mock<ILogger<ExecutionInitializer>>();
        var mockRouterBranchFilterService = new Mock<IRouterBranchFilterService>();

        // Setup default router branch filter to pass through all nodes (no filtering)
        mockRouterBranchFilterService.Setup(f =>
                f.FilterNodesAsync(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()))
            .ReturnsAsync((IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, Guid>? _) => new BranchFilterResult
            {
                IncludedNodes = nodes,
                ExcludedNodes = new List<Node>(),
                RouterSelections = new Dictionary<Guid, BranchSelection>()
            });

        _initializer = new ExecutionInitializer(
            _mockProcedureRepository.Object,
            _mockTimingOrchestrator.Object,
            mockAgentManager.Object,
            _mockIdAssigner.Object,
            _mockVariableResolver.Object,
            mockProcedureContext.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithProcedure_InitializesVariableContext()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var expectedContext = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, null))
            .ReturnsAsync(expectedContext);

        // Act
        var result = await _initializer.InitializeAsync(procedure.Id, executionId, executionStartTime);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VariableContext);
        Assert.Equal(executionId, result.VariableContext.ProcedureExecutionId);
        _mockVariableResolver.Verify(
            r => r.InitializeContextAsync(executionId, procedure, null),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithUserProvidedValues_PassesToVariableResolver()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var userValues = new Dictionary<string, object>
        {
            { "inputParam", 42 },
            { "userChoice", "Option A" }
        };
        var expectedContext = new VariableContext
        {
            ProcedureExecutionId = executionId
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, userValues))
            .ReturnsAsync(expectedContext);

        // Act
        var result = await _initializer.InitializeAsync(
            procedure.Id,
            executionId,
            executionStartTime,
            userValues);

        // Assert
        Assert.True(result.Success);
        _mockVariableResolver.Verify(
            r => r.InitializeContextAsync(executionId, procedure, userValues),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithDefaultVariableValues_CreatesContext()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId
        };
        context.SetValue("defaultVar", 100);

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, null))
            .ReturnsAsync(context);

        // Act
        var result = await _initializer.InitializeAsync(procedure.Id, executionId, executionStartTime);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VariableContext);
        Assert.Equal(100, result.VariableContext.GetValue<int>("defaultVar"));
    }

    [Fact]
    public async Task InitializeAsync_UserValuesOverrideDefaults()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var userValues = new Dictionary<string, object> { { "defaultVar", 200 } };
        var context = new VariableContext
        {
            ProcedureExecutionId = executionId
        };
        context.SetValue("defaultVar", 200); // User override

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, userValues))
            .ReturnsAsync(context);

        // Act
        var result = await _initializer.InitializeAsync(
            procedure.Id,
            executionId,
            executionStartTime,
            userValues);

        // Assert
        Assert.Equal(200, result.VariableContext!.GetValue<int>("defaultVar"));
    }

    [Fact]
    public async Task InitializeAsync_MissingRequiredVariable_ThrowsException()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, null))
            .ThrowsAsync(new InvalidOperationException("Required variable 'requiredVar' is missing"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _initializer.InitializeAsync(procedure.Id, executionId, executionStartTime));
        Assert.Contains("Required variable", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_InvalidVariableType_ThrowsException()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var userValues = new Dictionary<string, object> { { "numberVar", "not a number" } };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, userValues))
            .ThrowsAsync(new InvalidCastException("Cannot convert string to int"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidCastException>(() => _initializer.InitializeAsync(
            procedure.Id,
            executionId,
            executionStartTime,
            userValues));
        Assert.Contains("Cannot convert", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_ContextPersistedToRepository()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var context = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, null))
            .ReturnsAsync(context);

        // Act
        var result = await _initializer.InitializeAsync(procedure.Id, executionId, executionStartTime);

        // Assert
        Assert.NotNull(result.VariableContext);
        // The VariableResolver is responsible for persisting, so we just verify it was called
        _mockVariableResolver.Verify(
            r => r.InitializeContextAsync(executionId, procedure, null),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_MultipleExecutions_CreateSeparateContexts()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId1 = Guid.NewGuid();
        var executionId2 = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;

        var context1 = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId1
        };
        var context2 = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId2
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId1, procedure, null))
            .ReturnsAsync(context1);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId2, procedure, null))
            .ReturnsAsync(context2);

        // Act
        var result1 = await _initializer.InitializeAsync(procedure.Id, executionId1, executionStartTime);
        var result2 = await _initializer.InitializeAsync(procedure.Id, executionId2, executionStartTime);

        // Assert
        Assert.NotEqual(result1.VariableContext!.Id, result2.VariableContext!.Id);
        Assert.Equal(executionId1, result1.VariableContext.ProcedureExecutionId);
        Assert.Equal(executionId2, result2.VariableContext.ProcedureExecutionId);
    }

    [Fact]
    public async Task InitializeAsync_ReadOnlyVariables_CannotBeOverriddenByUser()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var userValues = new Dictionary<string, object> { { "readOnlyVar", 999 } };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, userValues))
            .ThrowsAsync(new InvalidOperationException("Cannot override read-only variable 'readOnlyVar'"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _initializer.InitializeAsync(
            procedure.Id,
            executionId,
            executionStartTime,
            userValues));
        Assert.Contains("read-only", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_ProcedureWithNoVariables_CreatesEmptyContext()
    {
        // Arrange
        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            Name = "No Variables Procedure",
            Variables = new List<VariableDefinition>(), // Empty
            RootNodeIds = new List<Guid>()
        };
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;
        var emptyContext = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId, procedure, null))
            .ReturnsAsync(emptyContext);

        // Act
        var result = await _initializer.InitializeAsync(procedure.Id, executionId, executionStartTime);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.VariableContext);
        Assert.Empty(result.VariableContext.GetAllValues());
    }

    [Fact]
    public async Task InitializeAsync_ContextIdIsUniquePerExecution()
    {
        // Arrange
        var procedure = CreateProcedureWithVariables();
        var executionId1 = Guid.NewGuid();
        var executionId2 = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;

        var context1 = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId1
        };
        var context2 = new VariableContext
        {
            Id = Guid.NewGuid(),
            ProcedureExecutionId = executionId2
        };

        SetupBasicMocks(procedure);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId1, procedure, null))
            .ReturnsAsync(context1);
        _mockVariableResolver
            .Setup(r => r.InitializeContextAsync(executionId2, procedure, null))
            .ReturnsAsync(context2);

        // Act
        var result1 = await _initializer.InitializeAsync(procedure.Id, executionId1, executionStartTime);
        var result2 = await _initializer.InitializeAsync(procedure.Id, executionId2, executionStartTime);

        // Assert
        Assert.NotEqual(Guid.Empty, result1.VariableContext!.Id);
        Assert.NotEqual(Guid.Empty, result2.VariableContext!.Id);
        Assert.NotEqual(result1.VariableContext.Id, result2.VariableContext.Id);
    }

    // Helper methods
    private void SetupBasicMocks(Procedure procedure)
    {
        var nodes = new List<Node>().AsReadOnly();
        var edges = new List<DependencyEdge>().AsReadOnly();

        _mockProcedureRepository
            .Setup(r => r.GetByIdAsync(procedure.Id))
            .ReturnsAsync(procedure);
        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(procedure.Id))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(procedure.Id))
            .ReturnsAsync([.. edges]);
        _mockIdAssigner
            .Setup(a => a.AssignExecutionIds(nodes))
            .Returns(nodes);

        var scheduleResult = new ScheduleResult
        {
            Success = true,
            NodeSchedules = new List<NodeSchedule>(),
            UpdatedNodes = nodes
        };
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);
    }

    private static Procedure CreateProcedureWithVariables()
    {
        return new Procedure
        {
            Id = Guid.NewGuid(),
            Name = "Test Procedure with Variables",
            Description = "Test procedure",
            Variables = new List<VariableDefinition>
            {
                new()
                {
                    Name = "inputParam",
                    Type = new NumberType(),
                    DefaultValue = 0,
                    Scope = VariableScope.Procedure
                },
                new()
                {
                    Name = "userChoice",
                    Type = new StringType(),
                    DefaultValue = "Default",
                    Scope = VariableScope.Procedure
                },
                new()
                {
                    Name = "defaultVar",
                    Type = new NumberType(),
                    DefaultValue = 100,
                    Scope = VariableScope.Procedure
                },
                new()
                {
                    Name = "readOnlyVar",
                    Type = new NumberType(),
                    DefaultValue = 42,
                    Scope = VariableScope.Procedure,
                    IsReadOnly = true
                }
            },
            RootNodeIds = new List<Guid>()
        };
    }
}