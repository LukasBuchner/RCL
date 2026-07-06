using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Application.Services.Branching;
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
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Initialization;

/// <summary>
///     Unit tests for ExecutionInitializer service.
/// </summary>
public class ExecutionInitializerTests
{
    private readonly ExecutionInitializer _initializer;
    private readonly Mock<IAgentManager> _mockAgentManager;
    private readonly Mock<IBranchSelector> _mockBranchSelector;
    private readonly Mock<IExecutionIdAssigner> _mockIdAssigner;
    private readonly Mock<IProcedureRepository> _mockProcedureRepository;
    private readonly Mock<IRouterBranchFilterService> _mockRouterBranchFilterService;
    private readonly Mock<ITimingCalculationOrchestrator> _mockTimingOrchestrator;
    private readonly Mock<IVariableResolver> _mockVariableResolver;
    private readonly Procedure _testProcedure;

    private readonly Guid _testProcedureId = Guid.NewGuid();

    public ExecutionInitializerTests()
    {
        _mockTimingOrchestrator = new Mock<ITimingCalculationOrchestrator>();
        _mockAgentManager = new Mock<IAgentManager>();
        _mockIdAssigner = new Mock<IExecutionIdAssigner>();
        _mockBranchSelector = new Mock<IBranchSelector>();
        _mockRouterBranchFilterService = new Mock<IRouterBranchFilterService>();

        _mockProcedureRepository = new Mock<IProcedureRepository>();
        _mockVariableResolver = new Mock<IVariableResolver>();
        var mockProcedureContext = new Mock<IProcedureContext>();
        var mockLogger = new Mock<ILogger<ExecutionInitializer>>();

        // Create test procedure
        _testProcedure = new Procedure
        {
            Id = _testProcedureId,
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>(),
            Variables = new List<VariableDefinition>()
        };

        // Setup default procedure context behavior
        mockProcedureContext.Setup(pc => pc.RequireCurrentProcedureId())
            .Returns(_testProcedureId);
        mockProcedureContext.Setup(pc => pc.CurrentProcedureId)
            .Returns(_testProcedureId);

        // Setup procedure repository to return the test procedure
        _mockProcedureRepository.Setup(r => r.GetByIdAsync(_testProcedureId))
            .ReturnsAsync(_testProcedure);

        // Setup default repository returns (empty lists)
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);

        // Setup variable resolver to return a valid context
        _mockVariableResolver.Setup(r => r.InitializeContextAsync(
                It.IsAny<Guid>(),
                _testProcedure,
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((Guid executionId, Procedure _,
                    Dictionary<string, object>? _) =>
                new VariableContext
                {
                    Id = Guid.NewGuid(),
                    ProcedureExecutionId = executionId
                });

        // Setup default router branch filter to pass through all nodes (no filtering)
        _mockRouterBranchFilterService.Setup(f =>
                f.FilterNodesAsync(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyDictionary<Guid, Guid>?>()))
            .ReturnsAsync((IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, Guid>? _) => new BranchFilterResult
            {
                IncludedNodes = nodes,
                ExcludedNodes = new List<Node>(),
                RouterSelections = new Dictionary<Guid, BranchSelection>()
            });

        // Setup default ID assigner to return nodes as-is (pass-through)
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(It.IsAny<IReadOnlyList<Node>>()))
            .Returns((IReadOnlyList<Node> nodes) => nodes);

        // Setup default timing orchestrator to return successful schedule
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchedulingRequest req, CancellationToken _) => new ScheduleResult
            {
                Success = true,
                NodeSchedules = new List<NodeSchedule>(),
                UpdatedNodes = req.Nodes
            });

        _initializer = new ExecutionInitializer(
            _mockProcedureRepository.Object,
            _mockTimingOrchestrator.Object,
            _mockAgentManager.Object,
            _mockIdAssigner.Object,
            _mockVariableResolver.Object,
            mockProcedureContext.Object,
            mockLogger.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var node = CreateSkillExecutionNode("Skill 1");
        var edge = CreateDependencyEdge(node.Id, node.Id);
        var nodes = new List<Node> { node }.AsReadOnly();
        var edges = new List<DependencyEdge> { edge }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. edges]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        var mockAgent = CreateMockAgent("Agent 1");
        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>())).Returns(mockAgent);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(nodes, result.Nodes);
        Assert.Equal(edges, result.Edges);
        Assert.NotNull(result.Schedule);
        Assert.Single(result.AgentAssignments);
        Assert.Equal(executionStartTime, result.ExecutionStartTime);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_LoadsNodesAndEdges()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();
        var edges = new List<DependencyEdge> { CreateDependencyEdge(Guid.NewGuid(), Guid.NewGuid()) }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. edges]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        _mockProcedureRepository.Verify(r => r.GetNodesByProcedureIdAsync(_testProcedureId), Times.Once);
        _mockProcedureRepository.Verify(r => r.GetEdgesByProcedureIdAsync(_testProcedureId), Times.Once);
        Assert.Equal(nodes.Count, result.Nodes.Count);
        Assert.Equal(edges.Count, result.Edges.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_AssignsExecutionIds()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var originalNodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();
        var nodesWithIds = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. originalNodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(originalNodes)).Returns(nodesWithIds);

        var scheduleResult = CreateSuccessfulScheduleResult(nodesWithIds);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        _mockIdAssigner.Verify(a => a.AssignExecutionIds(originalNodes), Times.Once);
        Assert.Equal(nodesWithIds, result.Nodes);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_CalculatesScheduleWithCorrectRequest()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();
        var edges = new List<DependencyEdge>().AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        SchedulingRequest? capturedRequest = null;
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SchedulingRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(scheduleResult);

        // Act
        await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(nodes, capturedRequest.Nodes);
        Assert.Equal(edges, capturedRequest.Edges);
        Assert.False(capturedRequest.StrictMode);
        Assert.True(capturedRequest.IncludeDetailedTiming);
        Assert.True(capturedRequest.PreserveOriginalTaskDurations);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithScheduleFailure_ReturnsFailureResult()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var failedSchedule = new ScheduleResult
        {
            Success = false,
            ErrorMessage = "Schedule calculation failed",
            NodeSchedules = new List<NodeSchedule>()
        };
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedSchedule);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.False(result.Success);
        Assert.StartsWith("Failed to calculate initial schedule for procedure", result.ErrorMessage);
        Assert.NotNull(result.Schedule);
        Assert.False(result.Schedule.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_BuildsAgentAssignmentsFromSchedule()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var nodes = new List<Node> { skillNode1, skillNode2 }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        var agent1 = CreateMockAgent("Agent 1");
        var agent2 = CreateMockAgent("Agent 2");
        _mockAgentManager.Setup(m => m.GetAgent(skillNode1.SkillExecutionTask.AgentId)).Returns(agent1);
        _mockAgentManager.Setup(m => m.GetAgent(skillNode2.SkillExecutionTask.AgentId)).Returns(agent2);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.Equal(2, result.AgentAssignments.Count);
        Assert.Equal(agent1, result.AgentAssignments[skillNode1.Id]);
        Assert.Equal(agent2, result.AgentAssignments[skillNode2.Id]);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithMissingAgent_ExcludesFromAssignments()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>())).Returns((IRuntimeAgent?)null);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.Empty(result.AgentAssignments);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_IgnoresNonSkillExecutionNodes()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var taskNode = CreateTaskNode("Task 1");
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        var mockAgent = CreateMockAgent("Agent 1");
        _mockAgentManager.Setup(m => m.GetAgent(It.IsAny<Guid>())).Returns(mockAgent);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.Single(result.AgentAssignments); // Only skill node should have agent assignment
        Assert.Contains(skillNode.Id, result.AgentAssignments.Keys);
        Assert.DoesNotContain(taskNode.Id, result.AgentAssignments.Keys);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithRepositoryException_ReturnsFailureResult()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to initialize execution", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithTimingOrchestratorException_ReturnsFailureResult()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Timing calculation error"));

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to initialize execution", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_PassesCancellationTokenToOrchestrator()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node> { CreateSkillExecutionNode("Skill 1") }.AsReadOnly();
        var cancellationToken = new CancellationToken();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = CreateSuccessfulScheduleResult(nodes);
        _mockTimingOrchestrator.Setup(o =>
                o.CalculateAsync(It.IsAny<SchedulingRequest>(),
                    It.Is<CancellationToken>(ct => ct == cancellationToken)))
            .ReturnsAsync(scheduleResult);

        // Act
        await _initializer.InitializeAsync(executionStartTime, cancellationToken);

        // Assert
        _mockTimingOrchestrator.Verify(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_WithEmptyNodes_ReturnsSuccessWithNoAssignments()
    {
        // Arrange
        var executionStartTime = DateTimeOffset.UtcNow;
        var nodes = new List<Node>().AsReadOnly();
        new List<DependencyEdge>().AsReadOnly();

        _mockProcedureRepository.Setup(r => r.GetNodesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([.. nodes]);
        _mockProcedureRepository.Setup(r => r.GetEdgesByProcedureIdAsync(_testProcedureId))
            .ReturnsAsync([]);
        _mockIdAssigner.Setup(a => a.AssignExecutionIds(nodes)).Returns(nodes);

        var scheduleResult = new ScheduleResult
        {
            Success = true,
            NodeSchedules = new List<NodeSchedule>(),
            UpdatedNodes = nodes
        };
        _mockTimingOrchestrator
            .Setup(o => o.CalculateAsync(It.IsAny<SchedulingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleResult);

        // Act
        var result = await _initializer.InitializeAsync(executionStartTime);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Nodes);
        Assert.Empty(result.AgentAssignments);
    }

    // Helper methods
    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                ExecutionId = Guid.NewGuid(),
                Skill = CreateSkill(skillName)
            },
            ProcedureId = default
        };
    }

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

    private static DependencyEdge CreateDependencyEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            ProcedureId = default
        };
    }

    private static IRuntimeAgent CreateMockAgent(string name)
    {
        var mock = new Mock<IRuntimeAgent>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.Id).Returns(Guid.NewGuid());
        return mock.Object;
    }

    private static ScheduleResult CreateSuccessfulScheduleResult(IReadOnlyList<Node> nodes)
    {
        return new ScheduleResult
        {
            Success = true,
            NodeSchedules = nodes.Select(n => new NodeSchedule
            {
                NodeId = n.Id,
                Duration = 5.0,
                NodeType = n is SkillExecutionNode ? NodeScheduleType.SkillExecutionNode : NodeScheduleType.TaskNode
            }).ToList(),
            UpdatedNodes = nodes
        };
    }
}