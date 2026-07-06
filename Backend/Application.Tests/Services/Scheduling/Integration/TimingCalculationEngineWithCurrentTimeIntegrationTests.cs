using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Integration;

/// <summary>
///     Integration tests for TimingCalculationEngine with real ExecutionGraphBuilder
///     to verify currentTime flows through the entire pipeline correctly.
/// </summary>
public class TimingCalculationEngineWithCurrentTimeIntegrationTests
{
    private readonly List<Agent> _domainAgents;
    private readonly ISkillDurationProvider _durationProvider;
    private readonly IExecutionGraphBuilder _executionGraphBuilder;
    private readonly List<Mock<IRuntimeAgent>> _mockAgents;
    private readonly Mock<IAgentApplicationService> _mockAgentService;
    private readonly List<Skill> _skills;
    private readonly TimingCalculationEngine _timingCalculationEngine;

    public TimingCalculationEngineWithCurrentTimeIntegrationTests()
    {
        // Setup domain entities
        _skills = [];
        _domainAgents = [];
        _mockAgents = [];
        _mockAgentService = new Mock<IAgentApplicationService>();

        // Create agents and skills
        SetupAgentsAndSkills();

        // Setup agent service
        _mockAgentService.Setup(s => s.GetAgentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _domainAgents.FirstOrDefault(a => a.Id == id));

        // Create real ExecutionGraphBuilder with all dependencies
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(It.IsAny<Guid>()))
            .Returns((Guid id) => _mockAgents.Select(m => m.Object).FirstOrDefault(a => a.Id == id));
        var nodeAgentMapper = new NodeAgentMapper(
            mockAgentProvider.Object,
            Mock.Of<ILogger<NodeAgentMapper>>(),
            _mockAgentService.Object);

        var agentCapabilityAnalyzer = new AgentCapabilityAnalyzer(Mock.Of<ILogger<AgentCapabilityAnalyzer>>());
        var edgeTypeMapper = new EdgeTypeMapper();

        var builderHierarchyProcessor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(Mock.Of<ILogger<NodeRelationshipMapper>>()),
            new HierarchyValidator(Mock.Of<ILogger<HierarchyValidator>>()),
            Mock.Of<ILogger<NodeHierarchyProcessor>>());

        _executionGraphBuilder = new ExecutionGraphBuilder(
            Mock.Of<ILogger<ExecutionGraphBuilder>>(),
            edgeTypeMapper,
            builderHierarchyProcessor,
            new FHOOE.Freydis.Application.Services.Common.NodeResolver(
                Mock.Of<ILogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>>()));

        // Create duration provider for planning mode
        _durationProvider = new PlanningModeDurationProvider(
            nodeAgentMapper,
            agentCapabilityAnalyzer,
            Mock.Of<ILogger<PlanningModeDurationProvider>>());

        // Create real SchedulePlanner
        var schedulePlanner = new SchedulePlanner(Mock.Of<ILogger<SchedulePlanner>>());

        // Create supporting services for TimingCalculationEngine
        var mockChildNodeCollector = CreateMockChildNodeCollector();
        var mockTimingAggregator = CreateMockTimingAggregator();
        var mockHierarchicalSorter = CreateMockHierarchicalSorter();

        var taskNodeDurationCalculator = new TaskNodeDurationCalculator(
            mockChildNodeCollector,
            mockTimingAggregator,
            mockHierarchicalSorter,
            Mock.Of<ILogger<TaskNodeDurationCalculator>>());

        var routerNodeDurationCalculator = new RouterNodeDurationCalculator(
            Mock.Of<ILogger<RouterNodeDurationCalculator>>());

        var statisticsCollector = new TimingStatisticsCollector();
        var durationAdjuster = new NodeDurationAdjuster(
            taskNodeDurationCalculator,
            routerNodeDurationCalculator,
            mockChildNodeCollector,
            mockTimingAggregator,
            mockHierarchicalSorter,
            Mock.Of<ILogger<NodeDurationAdjuster>>());
        var timingMapper = new NodeTimingMapper(Mock.Of<ILogger<NodeTimingMapper>>());

        // Create TimingCalculationEngine with all real dependencies
        _timingCalculationEngine = new TimingCalculationEngine(
            _executionGraphBuilder,
            schedulePlanner,
            taskNodeDurationCalculator,
            statisticsCollector,
            durationAdjuster,
            timingMapper,
            new FHOOE.Freydis.Application.Services.Common.NodeResolver(
                Mock.Of<ILogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>>()),
            Mock.Of<ILogger<TimingCalculationEngine>>());
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithCurrentTime10_SchedulesTasksAfterCurrentTime()
    {
        // Arrange
        const double currentTime = 10.0;
        var (nodes, edges) = CreateSimpleProcedure();
        var nodeHierarchy = ProcessNodeHierarchy(nodes);
        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = _durationProvider
        };

        // Act
        var result = await _timingCalculationEngine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DetailedTimingInfo);
        Assert.NotEmpty(result.DetailedTimingInfo);

        // Verify all skill executions start at or after currentTime
        foreach (var (nodeId, timing) in result.DetailedTimingInfo)
            if (timing.NodeType == NodeTimingType.SkillExecution)
                Assert.True(timing.AbsoluteStartTime >= currentTime,
                    $"Skill execution {nodeId} started at {timing.AbsoluteStartTime}, " +
                    $"which is before currentTime {currentTime}");
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithCurrentTime50_AndDependencies_RespectsCurrentTime()
    {
        // Arrange
        const double currentTime = 50.0;
        var (nodes, edges) = CreateProcedureWithDependencies();
        var nodeHierarchy = ProcessNodeHierarchy(nodes);
        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = _durationProvider
        };

        // Act
        var result = await _timingCalculationEngine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DetailedTimingInfo);

        // Find the first task (no dependencies)
        var firstSkillTiming = result.DetailedTimingInfo.Values
            .Where(t => t.NodeType == NodeTimingType.SkillExecution)
            .OrderBy(t => t.AbsoluteStartTime)
            .First();

        // First task should start at currentTime
        Assert.Equal(currentTime, firstSkillTiming.AbsoluteStartTime, 2);

        // Verify all tasks respect currentTime and dependencies
        foreach (var timing in result.DetailedTimingInfo.Values.Where(t => t.NodeType == NodeTimingType.SkillExecution))
            Assert.True(timing.AbsoluteStartTime >= currentTime,
                $"Task started at {timing.AbsoluteStartTime} which is before currentTime {currentTime}");
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithCurrentTimeZero_SchedulesFromZero()
    {
        // Arrange
        const double currentTime = 0.0;
        var (nodes, edges) = CreateSimpleProcedure();
        var nodeHierarchy = ProcessNodeHierarchy(nodes);
        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = _durationProvider
        };

        // Act
        var result = await _timingCalculationEngine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DetailedTimingInfo);

        // With currentTime=0, first task should start at 0
        var firstSkillTiming = result.DetailedTimingInfo.Values
            .Where(t => t.NodeType == NodeTimingType.SkillExecution)
            .OrderBy(t => t.AbsoluteStartTime)
            .First();

        Assert.Equal(0.0, firstSkillTiming.AbsoluteStartTime, 2);
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithCurrentTime100_MultipleTasksAndDependencies_SchedulesCorrectly()
    {
        // Arrange
        const double currentTime = 100.0;
        var (nodes, edges) = CreateComplexProcedure();
        var nodeHierarchy = ProcessNodeHierarchy(nodes);
        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = _durationProvider
        };

        // Act
        var result = await _timingCalculationEngine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DetailedTimingInfo);
        Assert.True(result.DetailedTimingInfo.Count >= 3); // At least 3 skill executions

        // Verify sequential execution respects dependencies and currentTime
        var skillTimings = result.DetailedTimingInfo.Values
            .Where(t => t.NodeType == NodeTimingType.SkillExecution)
            .OrderBy(t => t.AbsoluteStartTime)
            .ToList();

        // First task starts at currentTime
        Assert.True(skillTimings[0].AbsoluteStartTime >= currentTime,
            $"First task started at {skillTimings[0].AbsoluteStartTime}, expected >= {currentTime}");

        // Subsequent tasks respect dependencies
        for (var i = 1; i < skillTimings.Count; i++)
        {
            var prevTiming = skillTimings[i - 1];
            var currTiming = skillTimings[i];

            // Current task should start after previous task finishes (or at the same time if parallel)
            Assert.True(currTiming.AbsoluteStartTime >= prevTiming.AbsoluteStartTime,
                $"Task {i} started at {currTiming.AbsoluteStartTime}, " +
                $"which is before task {i - 1} started at {prevTiming.AbsoluteStartTime}");
        }
    }

    [Fact]
    public async Task CalculateTimingsAsync_WithLargeCurrentTime_SchedulesAllTasksAfterCurrentTime()
    {
        // Arrange
        const double currentTime = 1000.0;
        var (nodes, edges) = CreateProcedureWithDependencies();
        var nodeHierarchy = ProcessNodeHierarchy(nodes);
        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = _durationProvider
        };

        // Act
        var result = await _timingCalculationEngine.CalculateTimingsAsync(nodeHierarchy, edges, options);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DetailedTimingInfo);

        // All skill executions should start at or after currentTime
        var allSkillTimings = result.DetailedTimingInfo.Values
            .Where(t => t.NodeType == NodeTimingType.SkillExecution)
            .ToList();

        foreach (var timing in allSkillTimings)
            Assert.True(timing.AbsoluteStartTime >= currentTime,
                $"Skill execution started at {timing.AbsoluteStartTime}, expected >= {currentTime}");

        // The earliest start should be at currentTime
        var earliestStart = allSkillTimings.Min(t => t.AbsoluteStartTime);
        Assert.Equal(currentTime, earliestStart, 2);
    }

    #region Helper Methods

    private void SetupAgentsAndSkills()
    {
        // Create skills
        var skill1 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "PickSkill",
            Description = "Pick operation",
            Properties = []
        };

        var skill2 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "PlaceSkill",
            Description = "Place operation",
            Properties = []
        };

        var skill3 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "MoveSkill",
            Description = "Move operation",
            Properties = []
        };

        _skills.AddRange([skill1, skill2, skill3]);

        // Create agents
        var agent1 = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot1",
            SkillIds = [skill1.Id, skill2.Id, skill3.Id],
            RepresentativeColor = "Blue"
        };

        _domainAgents.Add(agent1);

        // Setup mock runtime agents
        var mockAgent1 = new Mock<IRuntimeAgent>();
        mockAgent1.SetupGet(a => a.Id).Returns(agent1.Id);

        // Setup execution estimates for each skill
        mockAgent1.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockAgent1.Setup(a => a.GetExecutionEstimateAsync(skill1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillExecutionEstimate
            {
                Skill = skill1,
                AgentId = agent1.Id,
                CanExecuteAdaptively = false,
                EstimatedNominalDuration = 5.0
            });

        mockAgent1.Setup(a => a.GetExecutionEstimateAsync(skill2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillExecutionEstimate
            {
                Skill = skill2,
                AgentId = agent1.Id,
                CanExecuteAdaptively = false,
                EstimatedNominalDuration = 3.0
            });

        mockAgent1.Setup(a => a.GetExecutionEstimateAsync(skill3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillExecutionEstimate
            {
                Skill = skill3,
                AgentId = agent1.Id,
                CanExecuteAdaptively = false,
                EstimatedNominalDuration = 4.0
            });

        _mockAgents.Add(mockAgent1);
    }

    private (List<Node> nodes, List<DependencyEdge> edges) CreateSimpleProcedure()
    {
        var taskNode = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "SimpleTask",
                StartTime = 0,
                Duration = 5.0
            }
        };

        var skillNode = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "PickExecution",
                StartTime = 0,
                Duration = 5.0,
                Skill = _skills[0],
                AgentId = _domainAgents[0].Id
            }
        };

        var nodes = new List<Node> { taskNode, skillNode };
        var edges = new List<DependencyEdge>();

        return (nodes, edges);
    }

    private (List<Node> nodes, List<DependencyEdge> edges) CreateProcedureWithDependencies()
    {
        var taskNode1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "PickTask",
                StartTime = 0,
                Duration = 5.0
            }
        };

        var skillNode1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode1.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "PickExecution",
                StartTime = 0,
                Duration = 5.0,
                Skill = _skills[0],
                AgentId = _domainAgents[0].Id
            }
        };

        var taskNode2 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "PlaceTask",
                StartTime = 5,
                Duration = 3.0
            }
        };

        var skillNode2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode2.Id,
            Position = new NodePosition { X = 100, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "PlaceExecution",
                StartTime = 5,
                Duration = 3.0,
                Skill = _skills[1],
                AgentId = _domainAgents[0].Id
            }
        };

        // Dependency: PickTask -> PlaceTask (Finish-to-Start)
        var edge = new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillNode1.Id,
            TargetId = skillNode2.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        };

        var nodes = new List<Node> { taskNode1, skillNode1, taskNode2, skillNode2 };
        var edges = new List<DependencyEdge> { edge };

        return (nodes, edges);
    }

    private (List<Node> nodes, List<DependencyEdge> edges) CreateComplexProcedure()
    {
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();

        // Task 1: Pick
        var taskNode1 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "PickTask",
                StartTime = 0,
                Duration = 5.0
            }
        };

        var skillNode1 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode1.Id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "PickExecution",
                StartTime = 0,
                Duration = 5.0,
                Skill = _skills[0],
                AgentId = _domainAgents[0].Id
            }
        };

        // Task 2: Move
        var taskNode2 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 100, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "MoveTask",
                StartTime = 5,
                Duration = 4.0
            }
        };

        var skillNode2 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode2.Id,
            Position = new NodePosition { X = 100, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "MoveExecution",
                StartTime = 5,
                Duration = 4.0,
                Skill = _skills[2],
                AgentId = _domainAgents[0].Id
            }
        };

        // Task 3: Place
        var taskNode3 = new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 200, Y = 0 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "PlaceTask",
                StartTime = 9,
                Duration = 3.0
            }
        };

        var skillNode3 = new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            ParentId = taskNode3.Id,
            Position = new NodePosition { X = 200, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "PlaceExecution",
                StartTime = 9,
                Duration = 3.0,
                Skill = _skills[1],
                AgentId = _domainAgents[0].Id
            }
        };

        nodes.AddRange([taskNode1, skillNode1, taskNode2, skillNode2, taskNode3, skillNode3]);

        // Dependencies: Pick -> Move -> Place
        edges.Add(new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillNode1.Id,
            TargetId = skillNode2.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        });

        edges.Add(new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = skillNode2.Id,
            TargetId = skillNode3.Id,
            SourceHandle = "right",
            TargetHandle = "left"
        });

        return (nodes, edges);
    }

    private NodeHierarchyInfo ProcessNodeHierarchy(List<Node> nodes)
    {
        var mockRelationshipMapper = new Mock<INodeRelationshipMapper>();
        var mockHierarchyValidator = new Mock<IHierarchyValidator>();

        mockRelationshipMapper.Setup(x => x.BuildParentToChildrenMapping(It.IsAny<IReadOnlyList<Node>>()))
            .Returns((IReadOnlyList<Node> nodeList) =>
            {
                var mapping = new Dictionary<Guid, IReadOnlyList<Node>>();
                var grouped = nodeList.GroupBy(n => n.ParentId ?? Guid.Empty);
                foreach (var group in grouped) mapping[group.Key] = group.ToList().AsReadOnly();

                return mapping.AsReadOnly();
            });

        mockRelationshipMapper.Setup(x => x.BuildTaskToSkillMapping(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes, IReadOnlyList<SkillExecutionNode> skillNodes) =>
            {
                var mapping = new Dictionary<Guid, IReadOnlyList<SkillExecutionNode>>();
                foreach (var taskNode in taskNodes)
                {
                    var childSkills = skillNodes.Where(sn => sn.ParentId == taskNode.Id).ToList().AsReadOnly();
                    mapping[taskNode.Id] = childSkills;
                }

                return mapping.AsReadOnly();
            });

        mockRelationshipMapper.Setup(x => x.BuildSkillToTaskMapping(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes, IReadOnlyList<SkillExecutionNode> skillNodes) =>
            {
                var mapping = new Dictionary<Guid, TaskNode>();
                foreach (var skillNode in skillNodes)
                {
                    var parentTask = taskNodes.FirstOrDefault(t => t.Id == skillNode.ParentId);
                    if (parentTask != null) mapping[skillNode.Id] = parentTask;
                }

                return mapping.AsReadOnly();
            });

        mockHierarchyValidator.Setup(x => x.ValidateConsistency(
                It.IsAny<IReadOnlyList<TaskNode>>(),
                It.IsAny<IReadOnlyList<SkillExecutionNode>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<SkillExecutionNode>>>(),
                It.IsAny<IReadOnlyDictionary<Guid, TaskNode>>()))
            .Returns(new HierarchyValidationResult(true, [], []));

        var processor = new NodeHierarchyProcessor(
            mockRelationshipMapper.Object,
            mockHierarchyValidator.Object,
            Mock.Of<ILogger<NodeHierarchyProcessor>>());

        return processor.ProcessHierarchy(nodes.AsReadOnly());
    }

    private IChildNodeCollector CreateMockChildNodeCollector()
    {
        var mock = new Mock<IChildNodeCollector>();
        mock.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns((Guid parentId, IReadOnlyList<Node> allNodes) =>
            {
                var childNodes = allNodes.Where(n => n.ParentId == parentId).ToList();
                var skillNodes = childNodes.OfType<SkillExecutionNode>().ToList().AsReadOnly();
                var taskNodes = childNodes.OfType<TaskNode>().ToList().AsReadOnly();
                var routerNodes = childNodes.OfType<RouterNode>().ToList().AsReadOnly();
                return (skillNodes, taskNodes, routerNodes);
            });
        return mock.Object;
    }

    private ITimingAggregator CreateMockTimingAggregator()
    {
        var mock = new Mock<ITimingAggregator>();
        mock.Setup(x => x.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((IEnumerable<(double Duration, double StartTime, double FinishTime)> timings) =>
            {
                var timingList = timings.ToList();
                if (!timingList.Any()) return (0.0, 0.0, 0.0);
                var minStart = timingList.Min(t => t.StartTime);
                var maxFinish = timingList.Max(t => t.FinishTime);
                return (maxFinish - minStart, minStart, maxFinish);
            });
        return mock.Object;
    }

    private IHierarchicalSorter CreateMockHierarchicalSorter()
    {
        var mock = new Mock<IHierarchicalSorter>();
        mock.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes) => taskNodes.ToList());
        return mock.Object;
    }

    #endregion
}