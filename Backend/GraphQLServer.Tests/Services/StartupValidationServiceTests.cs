using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Services;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Tests.Services;

/// <summary>
///     Tests for the StartupValidationService to ensure proper validation logic at application startup.
/// </summary>
public class StartupValidationServiceTests
{
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<StartupValidationService>> _mockLogger;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public StartupValidationServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<StartupValidationService>>();
        _mockEnvironment = new Mock<IHostEnvironment>();

        // Enable all log levels so source-generated LoggerMessage methods actually call Log<TState>
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    [Fact]
    public async Task StartAsync_WhenNoProcedureLoaded_ShouldNotThrowException()
    {
        // Arrange - This test verifies that at startup, having no procedure loaded is a VALID state
        var mockAgentService = new Mock<IAgentApplicationService>();
        var mockSkillService = new Mock<ISkillApplicationService>();
        var mockNodeService = new Mock<INodeApplicationService>();
        var mockMapperService = new Mock<IGraphQlMapperService>();
        var mockExecutorResolver = new Mock<IRequestExecutorResolver>();
        var mockExecutor = new Mock<IRequestExecutor>();
        var mockSchema = new Mock<ISchema>();

        // Setup services - when no procedure is loaded, services should return empty collections
        // NOT throw exceptions
        mockAgentService.Setup(s => s.GetAllAgentsAsync()).ReturnsAsync(new List<Agent>());
        mockSkillService.Setup(s => s.GetAllSkillsAsync()).ReturnsAsync(new List<Skill>());

        // KEY TEST: GetAllNodesAsync should return empty list when no procedure is loaded,
        // NOT throw an exception
        mockNodeService.Setup(s => s.GetAllNodesAsync()).ReturnsAsync(new List<Node>());

        // Setup GraphQL schema
        var criticalTypes = new[]
        {
            CreateMockType("Query"),
            CreateMockType("Mutation"),
            CreateMockType("Subscription"),
            CreateMockType("Agent"),
            CreateMockType("Skill"),
            CreateMockType("Node")
        };
        mockSchema.Setup(s => s.Types).Returns(criticalTypes);
        mockExecutor.Setup(e => e.Schema).Returns(mockSchema.Object);
        mockExecutorResolver.Setup(r => r.GetRequestExecutorAsync(null, default))
            .ReturnsAsync(mockExecutor.Object);

        // Setup service resolution
        SetupService(typeof(IAgentApplicationService), mockAgentService.Object);
        SetupService(typeof(ISkillApplicationService), mockSkillService.Object);
        SetupService(typeof(INodeApplicationService), mockNodeService.Object);
        SetupService(typeof(IGraphQlMapperService), mockMapperService.Object);
        // Don't register PostgresDbContext - unit tests don't have PostgreSQL available
        // The validation will detect it's not registered which is expected in tests
        SetupService(typeof(PostgresDbContext), null);
        SetupService(typeof(IRequestExecutorResolver), mockExecutorResolver.Object);

        // Also setup required services for validation
        SetupService(
            typeof(IDependencyEdgeApplicationService),
            Mock.Of<IDependencyEdgeApplicationService>());
        SetupService(
            typeof(IPositionTagApplicationService),
            Mock.Of<IPositionTagApplicationService>());
        SetupService(
            typeof(ISceneObjectApplicationService),
            Mock.Of<ISceneObjectApplicationService>());

        // Don't fail fast in test - we want to validate the behavior
        // IsDevelopment() is an extension method, so mock EnvironmentName instead
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        var validationService = new StartupValidationService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - This should NOT throw an exception in Production mode
        var exception = await Record.ExceptionAsync(() => validationService.StartAsync(default));

        // Assert
        Assert.Null(exception);

        // Verify that the node service was called - proving that having no procedure loaded is valid
        mockNodeService.Verify(s => s.GetAllNodesAsync(), Times.Once);

        // In Production mode, even if there are validation errors (like PostgreSQL not being registered in tests),
        // the service should not throw. We verify the application services were validated successfully.
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => (o.ToString() ?? "").Contains("Node service validated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenNodeServiceThrowsException_ShouldCaptureError()
    {
        // Arrange - If the service throws an unexpected exception (not related to procedure context),
        // it should be captured as a validation error
        var mockAgentService = new Mock<IAgentApplicationService>();
        var mockSkillService = new Mock<ISkillApplicationService>();
        var mockNodeService = new Mock<INodeApplicationService>();
        var mockMapperService = new Mock<IGraphQlMapperService>();
        var mockExecutorResolver = new Mock<IRequestExecutorResolver>();
        var mockExecutor = new Mock<IRequestExecutor>();
        var mockSchema = new Mock<ISchema>();

        // Setup services
        mockAgentService.Setup(s => s.GetAllAgentsAsync()).ReturnsAsync(new List<Agent>());
        mockSkillService.Setup(s => s.GetAllSkillsAsync()).ReturnsAsync(new List<Skill>());

        // Node service throws a REAL error (not procedure context related)
        mockNodeService.Setup(s => s.GetAllNodesAsync())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Setup GraphQL schema
        var criticalTypes = new[]
        {
            CreateMockType("Query"),
            CreateMockType("Mutation"),
            CreateMockType("Subscription"),
            CreateMockType("Agent"),
            CreateMockType("Skill"),
            CreateMockType("Node")
        };
        mockSchema.Setup(s => s.Types).Returns(criticalTypes);
        mockExecutor.Setup(e => e.Schema).Returns(mockSchema.Object);
        mockExecutorResolver.Setup(r => r.GetRequestExecutorAsync(null, default))
            .ReturnsAsync(mockExecutor.Object);

        // Setup service resolution
        SetupService(typeof(IAgentApplicationService), mockAgentService.Object);
        SetupService(typeof(ISkillApplicationService), mockSkillService.Object);
        SetupService(typeof(INodeApplicationService), mockNodeService.Object);
        SetupService(typeof(IGraphQlMapperService), mockMapperService.Object);
        // Don't register PostgresDbContext - unit tests don't have PostgreSQL available
        SetupService(typeof(PostgresDbContext), null);
        SetupService(typeof(IRequestExecutorResolver), mockExecutorResolver.Object);

        SetupService(
            typeof(IDependencyEdgeApplicationService),
            Mock.Of<IDependencyEdgeApplicationService>());
        SetupService(
            typeof(IPositionTagApplicationService),
            Mock.Of<IPositionTagApplicationService>());
        SetupService(
            typeof(ISceneObjectApplicationService),
            Mock.Of<ISceneObjectApplicationService>());

        // IsDevelopment() is an extension method, so mock EnvironmentName instead
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        var validationService = new StartupValidationService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act & Assert - Should throw because there's a real validation error
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => validationService.StartAsync(default));

        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Database connection failed", exception.Message);
    }

    private void SetupService(Type serviceType, object? serviceInstance)
    {
        _mockServiceProvider.Setup(p => p.GetService(serviceType)).Returns(serviceInstance);
    }

    private static INamedType CreateMockType(string name)
    {
        var mock = new Mock<INamedType>();
        mock.Setup(t => t.Name).Returns(name);
        return mock.Object;
    }
}