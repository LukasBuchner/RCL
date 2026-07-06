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
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Integration;

/// <summary>
///     Schedule-neutrality of an empty compound task on a dependency chain, for procedure <c>a63b7266</c>
///     (chain <c>Grasp(running) → dcd(empty) → jkl(Move) → …</c>).
///     <para>
///         An empty compound task is a zero-extent finish-to-start passthrough: inserting <c>Grasp → dcd →
///         Move</c> instead of <c>Grasp → Move</c> must not change the schedule of any real node. Each test
///         builds the SAME procedure twice — once with the empty compound task on the chain, once without it
///         (a direct edge) — reusing identical node IDs for the shared nodes, and asserts the shared nodes'
///         absolute start/finish times are identical, in both planning and execution mode.
///     </para>
/// </summary>
public class EmptyCompoundTaskScheduleNeutralityTests
{
    private readonly Agent _agent;
    private readonly Skill _graspSkill;
    private readonly Mock<IRuntimeAgent> _mockAgent;
    private readonly Skill _moveSkill;
    private readonly Skill _weldSkill;
    private readonly TimingCalculationEngine _timingCalculationEngine;

    public EmptyCompoundTaskScheduleNeutralityTests()
    {
        _graspSkill = new Skill
        { Id = Guid.NewGuid(), Name = "Grasp Object", Description = "Grasp", Properties = [] };
        _moveSkill = new Skill
        { Id = Guid.NewGuid(), Name = "Move Object To", Description = "Move", Properties = [] };
        _weldSkill = new Skill
        { Id = Guid.NewGuid(), Name = "Weld", Description = "Weld", Properties = [] };

        _agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot1",
            SkillIds = [_graspSkill.Id, _moveSkill.Id, _weldSkill.Id],
            RepresentativeColor = "Blue"
        };

        _mockAgent = new Mock<IRuntimeAgent>();
        _mockAgent.SetupGet(a => a.Id).Returns(_agent.Id);
        _mockAgent.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetupEstimate(_graspSkill, 45.0);
        SetupEstimate(_moveSkill, 65.0);
        SetupEstimate(_weldSkill, 90.0);

        _timingCalculationEngine = BuildEngine();
    }

    /// <summary>
    ///     Planning mode (no execution, currentTime = 0): inserting an empty compound task on the chain must
    ///     not move any real node.
    /// </summary>
    [Fact]
    public async Task EmptyCompoundTask_PlanningMode_DoesNotChangeSharedSchedule()
    {
        await AssertEmptyCompoundTaskIsScheduleNeutral(false);
    }

    /// <summary>
    ///     Execution mode (the running <c>Grasp</c> skill reports live progress, currentTime &gt; 0): inserting
    ///     an empty compound task on the chain must not move any real node. This is the configuration in which
    ///     the defect was observed; expected RED until fixed.
    /// </summary>
    [Fact]
    public async Task EmptyCompoundTask_ExecutionMode_DoesNotChangeSharedSchedule()
    {
        await AssertEmptyCompoundTaskIsScheduleNeutral(true);
    }

    /// <summary>
    ///     Builds the chain twice (with and without the empty compound task), runs both through the real
    ///     timing pipeline, and asserts every shared real node has identical absolute start/finish.
    /// </summary>
    /// <param name="running">When true, the Grasp skill is mid-execution with live progress data.</param>
    private async Task AssertEmptyCompoundTaskIsScheduleNeutral(bool running)
    {
        // Stable IDs shared by both procedures so timings are comparable by ID.
        var graspId = Guid.NewGuid();
        var moveId = Guid.NewGuid();
        var weldId = Guid.NewGuid();
        var graspExecutionId = Guid.NewGuid();

        SkillExecutionNode Grasp()
        {
            return CreateSkillNode(graspId, "sdf", _graspSkill, 0, 45,
                running ? graspExecutionId : null);
        }

        SkillExecutionNode Move()
        {
            return CreateSkillNode(moveId, "jkl", _moveSkill, 45, 65);
        }

        SkillExecutionNode Weld()
        {
            return CreateSkillNode(weldId, "Weld Component", _weldSkill, 110, 90);
        }

        // WITHOUT the empty compound task: Grasp --FS--> Move --FS--> Weld
        var without = await CalculateAsync(
            [Grasp(), Move(), Weld()],
            [
                CreateFsEdge(graspId, moveId),
                CreateFsEdge(moveId, weldId)
            ],
            running, graspExecutionId);

        // WITH the empty compound task: Grasp --FS--> dcd(empty) --FS--> Move --FS--> Weld
        var emptyId = Guid.NewGuid();
        var with = await CalculateAsync(
            [Grasp(), CreateEmptyCompoundTask(emptyId), Move(), Weld()],
            [
                CreateFsEdge(graspId, emptyId),
                CreateFsEdge(emptyId, moveId),
                CreateFsEdge(moveId, weldId)
            ],
            running, graspExecutionId);

        Assert.True(without.Success && with.Success);
        Assert.NotNull(without.DetailedTimingInfo);
        Assert.NotNull(with.DetailedTimingInfo);

        foreach (var (id, label) in new[] { (graspId, "Grasp"), (moveId, "Move"), (weldId, "Weld") })
        {
            var a = without.DetailedTimingInfo![id];
            var b = with.DetailedTimingInfo![id];
            Assert.True(Math.Abs(a.AbsoluteStartTime - b.AbsoluteStartTime) < 1e-6,
                $"{label} start differs: without-empty={a.AbsoluteStartTime}, with-empty={b.AbsoluteStartTime}");
            Assert.True(Math.Abs(a.AbsoluteFinishTime - b.AbsoluteFinishTime) < 1e-6,
                $"{label} finish differs: without-empty={a.AbsoluteFinishTime}, with-empty={b.AbsoluteFinishTime}");
        }
    }

    private async Task<TimingResult> CalculateAsync(
        List<Node> nodes, List<DependencyEdge> edges, bool running, Guid graspExecutionId,
        double estimatedTotalDuration = 48.0, double elapsedSeconds = 10.0)
    {
        var procedureStartUtc = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var currentTime = running ? elapsedSeconds : 0.0;

        ISkillDurationProvider durationProvider;
        if (running)
        {
            // Grasp is mid-execution: the estimate is supplied by the caller so a test can hold it fixed (to
            // isolate the empty task's structural effect) or vary it between recomputes (to reproduce jitter).
            var progressData = new Dictionary<Guid, SkillExecutionProgress>
            {
                [graspExecutionId] = new()
                {
                    ExecutionId = graspExecutionId,
                    SkillId = _graspSkill.Id,
                    AgentId = _agent.Id,
                    ActualStartTimeUtc = procedureStartUtc,
                    CurrentTimeIntoExecution = elapsedSeconds,
                    EstimatedTotalDuration = estimatedTotalDuration,
                    MinAchievableDuration = null,
                    CompletedSuccessfully = false,
                    Error = null,
                    StatusMessage = ""
                }
            };
            durationProvider = new ExecutionAwareDurationProvider(
                BuildPlanningProvider(), procedureStartUtc, progressData,
                Mock.Of<ILogger<ExecutionAwareDurationProvider>>());
        }
        else
        {
            durationProvider = BuildPlanningProvider();
        }

        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(),
            CurrentTime = currentTime,
            StrictMode = false,
            IncludeDetailedTiming = true,
            DurationProvider = durationProvider
        };

        return await _timingCalculationEngine.CalculateTimingsAsync(ProcessNodeHierarchy(nodes), edges, options);
    }

    private void SetupEstimate(Skill skill, double nominal)
    {
        _mockAgent.Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillExecutionEstimate
            {
                Skill = skill, AgentId = _agent.Id, CanExecuteAdaptively = false,
                EstimatedNominalDuration = nominal
            });
    }

    private SkillExecutionNode CreateSkillNode(
        Guid id, string name, Skill skill, double start, double duration, Guid? executionId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = start,
                Duration = duration,
                Skill = skill,
                AgentId = _agent.Id,
                ExecutionId = executionId
            }
        };
    }

    private static TaskNode CreateEmptyCompoundTask(Guid id)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id,
            Position = new NodePosition { X = 90, Y = 60 },
            Task = new DomainTask { Name = "dcd", StartTime = 0, Duration = 0 }
        };
    }

    private static DependencyEdge CreateFsEdge(Guid source, Guid target)
    {
        return new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = source,
            TargetId = target,
            SourceHandle = "right",
            TargetHandle = "left"
        };
    }

    private TimingCalculationEngine BuildEngine()
    {
        var edgeTypeMapper = new EdgeTypeMapper();
        var nodeResolver = new FHOOE.Freydis.Application.Services.Common.NodeResolver(
            Mock.Of<ILogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>>());

        var builderHierarchyProcessor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(Mock.Of<ILogger<NodeRelationshipMapper>>()),
            new HierarchyValidator(Mock.Of<ILogger<HierarchyValidator>>()),
            Mock.Of<ILogger<NodeHierarchyProcessor>>());

        var executionGraphBuilder = new ExecutionGraphBuilder(
            Mock.Of<ILogger<ExecutionGraphBuilder>>(), edgeTypeMapper, builderHierarchyProcessor, nodeResolver);

        var schedulePlanner = new SchedulePlanner(Mock.Of<ILogger<SchedulePlanner>>());

        var childNodeCollector = CreateMockChildNodeCollector();
        var timingAggregator = CreateMockTimingAggregator();
        var hierarchicalSorter = CreateMockHierarchicalSorter();

        var taskNodeDurationCalculator = new TaskNodeDurationCalculator(
            childNodeCollector, timingAggregator, hierarchicalSorter,
            Mock.Of<ILogger<TaskNodeDurationCalculator>>());
        var routerNodeDurationCalculator = new RouterNodeDurationCalculator(
            Mock.Of<ILogger<RouterNodeDurationCalculator>>());
        var durationAdjuster = new NodeDurationAdjuster(
            taskNodeDurationCalculator, routerNodeDurationCalculator, childNodeCollector, timingAggregator,
            hierarchicalSorter, Mock.Of<ILogger<NodeDurationAdjuster>>());

        return new TimingCalculationEngine(
            executionGraphBuilder,
            schedulePlanner,
            taskNodeDurationCalculator,
            new TimingStatisticsCollector(),
            durationAdjuster,
            new NodeTimingMapper(Mock.Of<ILogger<NodeTimingMapper>>()),
            nodeResolver,
            Mock.Of<ILogger<TimingCalculationEngine>>());
    }

    private ISkillDurationProvider BuildPlanningProvider()
    {
        var mockAgentService = new Mock<IAgentApplicationService>();
        mockAgentService.Setup(s => s.GetAgentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => id == _agent.Id ? _agent : null);

        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(It.IsAny<Guid>())).Returns(_mockAgent.Object);

        var nodeAgentMapper = new NodeAgentMapper(
            mockAgentProvider.Object, Mock.Of<ILogger<NodeAgentMapper>>(), mockAgentService.Object);
        var agentCapabilityAnalyzer = new AgentCapabilityAnalyzer(Mock.Of<ILogger<AgentCapabilityAnalyzer>>());

        return new PlanningModeDurationProvider(
            nodeAgentMapper, agentCapabilityAnalyzer, Mock.Of<ILogger<PlanningModeDurationProvider>>());
    }

    private static NodeHierarchyInfo ProcessNodeHierarchy(List<Node> nodes)
    {
        var processor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(Mock.Of<ILogger<NodeRelationshipMapper>>()),
            new HierarchyValidator(Mock.Of<ILogger<HierarchyValidator>>()),
            Mock.Of<ILogger<NodeHierarchyProcessor>>());
        return processor.ProcessHierarchy(nodes.AsReadOnly());
    }

    private static IChildNodeCollector CreateMockChildNodeCollector()
    {
        var mock = new Mock<IChildNodeCollector>();
        mock.Setup(x => x.CollectAllChildNodes(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Node>>()))
            .Returns((Guid parentId, IReadOnlyList<Node> allNodes) =>
            {
                var childNodes = allNodes.Where(n => n.ParentId == parentId).ToList();
                return (
                    childNodes.OfType<SkillExecutionNode>().ToList().AsReadOnly(),
                    childNodes.OfType<TaskNode>().ToList().AsReadOnly(),
                    childNodes.OfType<RouterNode>().ToList().AsReadOnly());
            });
        return mock.Object;
    }

    private static ITimingAggregator CreateMockTimingAggregator()
    {
        var mock = new Mock<ITimingAggregator>();
        mock.Setup(x => x.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((IEnumerable<(double Duration, double StartTime, double FinishTime)> timings) =>
            {
                var list = timings.ToList();
                if (list.Count == 0) return (0.0, 0.0, 0.0);
                var minStart = list.Min(t => t.StartTime);
                var maxFinish = list.Max(t => t.FinishTime);
                return (maxFinish - minStart, minStart, maxFinish);
            });
        return mock.Object;
    }

    private static IHierarchicalSorter CreateMockHierarchicalSorter()
    {
        var mock = new Mock<IHierarchicalSorter>();
        mock.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes) => taskNodes.ToList());
        return mock.Object;
    }
}