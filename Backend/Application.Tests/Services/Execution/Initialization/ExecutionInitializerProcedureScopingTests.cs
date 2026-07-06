// Ported from ExecutionInitializerProcedureScopingTests.cs.disabled.
//
// API drift resolved:
//   - Replaced separate INodeRepository + IDependencyEdgeRepository + IRepository<Procedure>
//     mocks with a single Mock<IProcedureRepository> (the unified aggregate repository).
//   - Changed GetByProcedureIdAsync → GetNodesByProcedureIdAsync / GetEdgesByProcedureIdAsync.
//   - Changed GetAllAsync() → GetAllNodesAsync() / GetAllEdgesAsync() for the "never called" guards.
//   - Updated ExecutionInitializer constructor call to the current 7-parameter signature.
//   - Removed AgentIds from Skill initializers (field no longer exists on the record).
//   - Replaced positional VariableContext constructor (did not exist) with object initializer.
//   - Added GetByIdAsync + InitializeContextAsync mocks for the context-based overload tests,
//     because ExecutionInitializer.InitializeAsync(executionStartTime) also loads the Procedure
//     entity and initializes a variable context before returning.
//
// Tests 4 and 5 (procedure-ID-parameter overload scoping) are retained; their variable-context
// assertions are covered more deeply by ExecutionInitializerVariableContextTests, but the
// procedure-scoped-repo-method assertions here are distinct and valuable.

using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Execution.Initialization;
using FHOOE.Freydis.Application.Services.Execution.Utilities;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Initialization;

/// <summary>
///     Tests that verify <see cref="ExecutionInitializer" /> scopes every repository call to the
///     currently loaded procedure. These tests guard against cross-procedure data leaks that would
///     feed incorrect node/edge sets to the hard gate in <c>ExecutionOrchestrator</c>.
/// </summary>
public class ExecutionInitializerProcedureScopingTests
{
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;
    private readonly Mock<IAgentManager> _mockAgentManager;
    private readonly Mock<IExecutionIdAssigner> _mockIdAssigner;
    private readonly Mock<IVariableResolver> _mockVariableResolver;
    private readonly Mock<ILogger<ExecutionInitializer>> _mockLogger;
    private readonly Mock<IProcedureContext> _mockProcedureContext;

    private readonly Guid _procedureAId = Guid.NewGuid();
    private readonly Guid _procedureBId = Guid.NewGuid();

    /// <summary>
    ///     Initializes a new instance of <see cref="ExecutionInitializerProcedureScopingTests" />.
    ///     All mocks are created fresh for each test; only mocks that are shared across helper
    ///     methods are kept as fields.
    /// </summary>
    public ExecutionInitializerProcedureScopingTests()
    {
        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockAgentManager = new Mock<IAgentManager>();
        _mockIdAssigner = new Mock<IExecutionIdAssigner>();
        _mockVariableResolver = new Mock<IVariableResolver>();
        _mockLogger = new Mock<ILogger<ExecutionInitializer>>();
        _mockProcedureContext = new Mock<IProcedureContext>();
    }

    /// <summary>
    ///     Verifies that <see cref="ExecutionInitializer.InitializeAsync(DateTimeOffset, CancellationToken)" />
    ///     calls <see cref="IProcedureRepository.GetNodesByProcedureIdAsync" /> and
    ///     <see cref="IProcedureRepository.GetEdgesByProcedureIdAsync" /> with the ID obtained
    ///     from <see cref="IProcedureContext.RequireCurrentProcedureId" /> and that the returned
    ///     <see cref="ExecutionInitializationResult" /> contains only nodes belonging to that procedure.
    ///     This is the PRIMARY test for the procedure-scoping correctness.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_WithLoadedProcedure_LoadsOnlyThatProceduresNodes()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;

        _mockProcedureContext.Setup(pc => pc.RequireCurrentProcedureId())
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(pc => pc.CurrentProcedureId)
            .Returns(_procedureAId);

        var procedureANodes = new List<Node>
        {
            CreateSkillExecutionNode("Skill A1", _procedureAId),
            CreateSkillExecutionNode("Skill A2", _procedureAId)
        };

        var procedureAEdges = new List<DependencyEdge>
        {
            CreateDependencyEdge(procedureANodes[0].Id, procedureANodes[1].Id, _procedureAId)
        };

        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync(procedureANodes);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync(procedureAEdges);

        SetupProcedureAndVariableContext(_procedureAId);

        var nodesReadOnly = procedureANodes.AsReadOnly();
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(nodesReadOnly);

        var scheduleResult = CreateSuccessfulScheduleResult(nodesReadOnly);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>()))
            .Returns(CreateMockAgent("Agent 1"));

        var initializer = CreateInitializer();

        // Act
        var result = await initializer.InitializeAsync(executionStartTime);

        // Assert
        result.Success.Should().BeTrue("initialization should succeed for the loaded procedure");
        result.Nodes.Should().HaveCount(2, "only Procedure A's nodes should be loaded");

        // OnlyContain uses an expression tree; 'is' pattern matching is not allowed there.
        // Use All() + cast to assert procedure ownership without an expression tree.
        result.Nodes.All(n => (n as SkillExecutionNode)?.ProcedureId == _procedureAId)
            .Should().BeTrue("all returned nodes must belong to Procedure A");

        // Scoped methods called with the correct ID
        _mockProcedureRepository.Verify(
            r => r.GetNodesByProcedureIdAsync(_procedureAId),
            Times.Once,
            "must load nodes via the procedure-scoped query");
        _mockProcedureRepository.Verify(
            r => r.GetEdgesByProcedureIdAsync(_procedureAId),
            Times.Once,
            "must load edges via the procedure-scoped query");

        // Global unscoped queries must never be issued
        _mockProcedureRepository.Verify(
            r => r.GetAllNodesAsync(),
            Times.Never,
            "must NOT use the global node query");
        _mockProcedureRepository.Verify(
            r => r.GetAllEdgesAsync(),
            Times.Never,
            "must NOT use the global edge query");
    }

    /// <summary>
    ///     Verifies that when two procedures' data are configured in the repository mocks,
    ///     only the currently loaded procedure's nodes appear in the result — no cross-procedure
    ///     leak occurs.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_WithMultipleProcedures_LoadsOnlyCurrentProcedure()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;

        _mockProcedureContext.Setup(pc => pc.RequireCurrentProcedureId())
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(pc => pc.CurrentProcedureId)
            .Returns(_procedureAId);

        var procedureANodes = new List<Node>
        {
            CreateSkillExecutionNode("Skill A1", _procedureAId),
            CreateSkillExecutionNode("Skill A2", _procedureAId)
        };

        // Procedure B data is wired up in the mock but must never be returned to the caller.
        var procedureBNodes = new List<Node>
        {
            CreateSkillExecutionNode("Skill B1", _procedureBId),
            CreateSkillExecutionNode("Skill B2", _procedureBId),
            CreateSkillExecutionNode("Skill B3", _procedureBId)
        };

        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync(procedureANodes);
        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(_procedureBId))
            .ReturnsAsync(procedureBNodes);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(_procedureBId))
            .ReturnsAsync([]);

        SetupProcedureAndVariableContext(_procedureAId);

        var nodesReadOnly = procedureANodes.AsReadOnly();
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(nodesReadOnly);

        var scheduleResult = CreateSuccessfulScheduleResult(nodesReadOnly);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>()))
            .Returns(CreateMockAgent("Agent 1"));

        var initializer = CreateInitializer();

        // Act
        var result = await initializer.InitializeAsync(executionStartTime);

        // Assert
        result.Success.Should().BeTrue();
        result.Nodes.Should().HaveCount(2, "only Procedure A's 2 nodes should be loaded");

        // NotContain uses an expression tree; 'is' pattern matching is not allowed there.
        result.Nodes.Any(n => (n as SkillExecutionNode)?.ProcedureId == _procedureBId)
            .Should().BeFalse("Procedure B's nodes must not appear in the result");

        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_procedureAId), Times.Once);
        _mockProcedureRepository.Verify(
            r => r.GetNodesByProcedureIdAsync(_procedureBId),
            Times.Never,
            "Procedure B nodes must never be queried");
    }

    /// <summary>
    ///     Verifies that when <see cref="IProcedureContext.RequireCurrentProcedureId" /> throws
    ///     (no procedure loaded), <see cref="ExecutionInitializer.InitializeAsync(DateTimeOffset, CancellationToken)" />
    ///     returns a failure result and does not query any repository methods.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_WithoutLoadedProcedure_ThrowsException()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;

        _mockProcedureContext
            .Setup(pc => pc.RequireCurrentProcedureId())
            .Throws(new InvalidOperationException("No procedure is currently loaded"));

        // CurrentProcedureId is read in the catch block to build the error result; return null.
        _mockProcedureContext.Setup(pc => pc.CurrentProcedureId).Returns((Guid?)null);

        var initializer = CreateInitializer();

        // Act
        var result = await initializer.InitializeAsync(executionStartTime);

        // Assert
        result.Success.Should().BeFalse("initialization must fail when no procedure is loaded");
        result.ErrorMessage.Should().Contain("Failed to initialize execution",
            "error message must indicate initialization failure");

        // No repository method should ever be called
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockProcedureRepository.Verify(r => r.GetAllEdgesAsync(), Times.Never);
    }

    /// <summary>
    ///     Verifies that the <see cref="SchedulingRequest.ProcedureId" /> forwarded to the timing
    ///     orchestrator equals the ID returned by <see cref="IProcedureContext.RequireCurrentProcedureId" />
    ///     and is never <see cref="Guid.Empty" />.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_SetsCorrectProcedureIdInSchedulingRequest()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;

        _mockProcedureContext.Setup(pc => pc.RequireCurrentProcedureId())
            .Returns(_procedureAId);
        _mockProcedureContext.Setup(pc => pc.CurrentProcedureId)
            .Returns(_procedureAId);

        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1", _procedureAId) };
        var nodesReadOnly = nodes.AsReadOnly();

        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync(nodes);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(_procedureAId))
            .ReturnsAsync([]);

        SetupProcedureAndVariableContext(_procedureAId);

        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(nodesReadOnly);

        SchedulingRequest? capturedRequest = null;
        var scheduleResult = CreateSuccessfulScheduleResult(nodesReadOnly);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(scheduleResult);

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>()))
            .Returns(CreateMockAgent("Agent 1"));

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeAsync(executionStartTime);

        // Assert
        capturedRequest.Should().NotBeNull("a scheduling request must be created");
        capturedRequest!.ProcedureId.Should().Be(_procedureAId,
            "the scheduling request must carry the actual loaded procedure ID");
        capturedRequest.ProcedureId.Should().NotBe(Guid.Empty,
            "ProcedureId must never be Guid.Empty");
    }

    /// <summary>
    ///     Verifies that the explicit-procedure-ID overload
    ///     <see cref="ExecutionInitializer.InitializeAsync(Guid, Guid, DateTimeOffset, Dictionary{string,object}?, CancellationToken)" />
    ///     also calls only the procedure-scoped repository methods and never the global ones.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_WithProcedureIdParameter_LoadsOnlyThatProcedure()
    {
        // Arrange
        var procedureId = _procedureAId;
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;

        var procedure = new Procedure { Id = procedureId, Name = "Test Procedure", RootNodeIds = [] };
        _mockProcedureRepository.Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1", procedureId) };
        var nodesReadOnly = nodes.AsReadOnly();

        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(procedureId))
            .ReturnsAsync(nodes);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(procedureId))
            .ReturnsAsync([]);

        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(nodesReadOnly);

        var scheduleResult = CreateSuccessfulScheduleResult(nodesReadOnly);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        _mockVariableResolver
            .Setup(v => v.InitializeContextAsync(
                It.IsAny<Guid>(),
                It.IsAny<Procedure>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(new VariableContext { ProcedureExecutionId = executionId });

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>()))
            .Returns(CreateMockAgent("Agent 1"));

        var initializer = CreateInitializer();

        // Act
        var result = await initializer.InitializeAsync(procedureId, executionId, executionStartTime);

        // Assert
        result.Success.Should().BeTrue();

        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(procedureId), Times.Once);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(procedureId), Times.Once);
        _mockProcedureRepository.Verify(r => r.GetAllNodesAsync(), Times.Never);
        _mockProcedureRepository.Verify(r => r.GetAllEdgesAsync(), Times.Never);
    }

    /// <summary>
    ///     Verifies that the explicit-procedure-ID overload forwards the correct
    ///     <see cref="SchedulingRequest.ProcedureId" /> to the timing orchestrator.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task
        InitializeAsync_WithProcedureIdParameter_SetsCorrectProcedureIdInSchedulingRequest()
    {
        // Arrange
        var procedureId = _procedureAId;
        var executionId = Guid.NewGuid();
        var executionStartTime = DateTimeOffset.UtcNow;

        var procedure = new Procedure { Id = procedureId, Name = "Test Procedure", RootNodeIds = [] };
        _mockProcedureRepository.Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1", procedureId) };
        var nodesReadOnly = nodes.AsReadOnly();

        _mockProcedureRepository
            .Setup(r => r.GetNodesByProcedureIdAsync(procedureId))
            .ReturnsAsync(nodes);
        _mockProcedureRepository
            .Setup(r => r.GetEdgesByProcedureIdAsync(procedureId))
            .ReturnsAsync([]);

        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns(nodesReadOnly);

        SchedulingRequest? capturedRequest = null;
        var scheduleResult = CreateSuccessfulScheduleResult(nodesReadOnly);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(scheduleResult);

        _mockVariableResolver
            .Setup(v => v.InitializeContextAsync(
                It.IsAny<Guid>(),
                It.IsAny<Procedure>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(new VariableContext { ProcedureExecutionId = executionId });

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>()))
            .Returns(CreateMockAgent("Agent 1"));

        var initializer = CreateInitializer();

        // Act
        await initializer.InitializeAsync(procedureId, executionId, executionStartTime);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.ProcedureId.Should().Be(procedureId,
            "scheduling request must carry the actual procedure ID");
        capturedRequest.ProcedureId.Should().NotBe(Guid.Empty);
    }

    // --- Private helpers ---

    /// <summary>
    ///     Creates a <see cref="ExecutionInitializer" /> backed by the class-level mocks.
    /// </summary>
    /// <returns>A fully constructed <see cref="ExecutionInitializer" /> ready for testing.</returns>
    private ExecutionInitializer CreateInitializer()
    {
        return new ExecutionInitializer(
            _mockProcedureRepository.Object,
            _mockTimingOrchestrator.Object,
            _mockAgentManager.Object,
            _mockIdAssigner.Object,
            _mockVariableResolver.Object,
            _mockProcedureContext.Object,
            _mockLogger.Object);
    }

    /// <summary>
    ///     Configures <see cref="_mockProcedureRepository" /> and <see cref="_mockVariableResolver" />
    ///     so that the context-based overload of <see cref="IExecutionInitializer.InitializeAsync" />
    ///     can complete the procedure-entity and variable-context steps without throwing.
    /// </summary>
    /// <param name="procedureId">The procedure ID to stub.</param>
    private void SetupProcedureAndVariableContext(Guid procedureId)
    {
        var procedure = new Procedure { Id = procedureId, Name = "Procedure A", RootNodeIds = [] };
        _mockProcedureRepository
            .Setup(r => r.GetByIdAsync(procedureId))
            .ReturnsAsync(procedure);

        _mockVariableResolver
            .Setup(v => v.InitializeContextAsync(
                It.IsAny<Guid>(),
                It.IsAny<Procedure>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(new VariableContext { ProcedureExecutionId = Guid.NewGuid() });
    }

    /// <summary>
    ///     Creates a minimal <see cref="SkillExecutionNode" /> with its <see cref="Node.ProcedureId" />
    ///     explicitly set so that cross-procedure-leak assertions can distinguish node ownership.
    /// </summary>
    /// <param name="skillName">Display name of the skill, used for the task name.</param>
    /// <param name="procedureId">The procedure this node belongs to.</param>
    /// <returns>A new <see cref="SkillExecutionNode" /> associated with <paramref name="procedureId" />.</returns>
    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid procedureId)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                ExecutionId = Guid.NewGuid(),
                Skill = CreateSkill(skillName)
            }
        };
    }

    /// <summary>
    ///     Creates a minimal <see cref="Skill" /> with the given name.
    /// </summary>
    /// <param name="name">The skill name.</param>
    /// <returns>A new <see cref="Skill" /> instance.</returns>
    private static Skill CreateSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill",
            Properties = []
        };
    }

    /// <summary>
    ///     Creates a <see cref="DependencyEdge" /> connecting two nodes within a procedure.
    /// </summary>
    /// <param name="sourceId">ID of the source node.</param>
    /// <param name="targetId">ID of the target node.</param>
    /// <param name="procedureId">The procedure this edge belongs to.</param>
    /// <returns>A new <see cref="DependencyEdge" /> instance.</returns>
    private static DependencyEdge CreateDependencyEdge(Guid sourceId, Guid targetId, Guid procedureId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = procedureId,
            SourceId = sourceId,
            TargetId = targetId
        };
    }

    /// <summary>
    ///     Creates a mock <see cref="IRuntimeAgent" /> with the given display name.
    /// </summary>
    /// <param name="name">The agent's display name.</param>
    /// <returns>A mock <see cref="IRuntimeAgent" /> instance.</returns>
    private static IRuntimeAgent CreateMockAgent(string name)
    {
        var mock = new Mock<IRuntimeAgent>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.Id).Returns(Guid.NewGuid());
        return mock.Object;
    }

    /// <summary>
    ///     Builds a successful <see cref="ScheduleResult" /> referencing the provided nodes.
    /// </summary>
    /// <param name="nodes">The nodes to include in the schedule.</param>
    /// <returns>A <see cref="ScheduleResult" /> marked as successful.</returns>
    private static ScheduleResult CreateSuccessfulScheduleResult(IReadOnlyList<Node> nodes)
    {
        return new ScheduleResult
        {
            Success = true,
            NodeSchedules = nodes.Select(n => new NodeSchedule
            {
                NodeId = n.Id,
                Duration = 5.0,
                NodeType = n is SkillExecutionNode
                    ? NodeScheduleType.SkillExecutionNode
                    : NodeScheduleType.TaskNode
            }).ToList(),
            UpdatedNodes = nodes
        };
    }
}