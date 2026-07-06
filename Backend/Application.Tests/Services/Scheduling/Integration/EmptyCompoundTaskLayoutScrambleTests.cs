using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
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
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Integration;

/// <summary>
///     Reproduction of the scrambled displayed schedule reported for procedure <c>a63b7266</c>: during
///     execution, with an empty compound task on the chain, the timeline lays nodes out in the wrong vertical
///     order (the running <c>jkl</c> skill sinks below <c>Welding</c> instead of preceding it), whereas the
///     same procedure without the empty compound task lays out correctly.
///     <para>
///         Each test drives the full pipeline — timing calculation with live execution progress, then vertical
///         (Y) positioning — and asserts the vertical lane order matches the execution order
///         (<c>sdf</c> above <c>jkl</c> above <c>Welding</c>). The without-empty case is expected GREEN; the
///         with-empty case reproduces the defect.
///     </para>
/// </summary>
public class EmptyCompoundTaskLayoutScrambleTests
{
    private readonly Agent _agent;
    private readonly Skill _graspSkill;
    private readonly Skill _holdSkill;
    private readonly Skill _moveSkill;
    private readonly Mock<IRuntimeAgent> _mockAgent;
    private readonly NodePositionYCalculator _yCalculator;
    private readonly Skill _weldSkill;
    private readonly TimingCalculationEngine _timingCalculationEngine;

    public EmptyCompoundTaskLayoutScrambleTests()
    {
        _graspSkill = new Skill { Id = Guid.NewGuid(), Name = "Grasp Object", Description = "Grasp", Properties = [] };
        _moveSkill = new Skill { Id = Guid.NewGuid(), Name = "Move Object To", Description = "Move", Properties = [] };
        _weldSkill = new Skill { Id = Guid.NewGuid(), Name = "Weld", Description = "Weld", Properties = [] };
        _holdSkill = new Skill { Id = Guid.NewGuid(), Name = "Hold Position", Description = "Hold", Properties = [] };

        _agent = new Agent
        {
            Id = Guid.NewGuid(), Name = "Robot1",
            SkillIds = [_graspSkill.Id, _moveSkill.Id, _weldSkill.Id, _holdSkill.Id],
            RepresentativeColor = "Blue"
        };

        _mockAgent = new Mock<IRuntimeAgent>();
        _mockAgent.SetupGet(a => a.Id).Returns(_agent.Id);
        _mockAgent.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetupEstimate(_graspSkill, 45.0);
        SetupEstimate(_moveSkill, 65.0);
        SetupEstimate(_weldSkill, 90.0);
        SetupEstimate(_holdSkill, 90.0);

        _timingCalculationEngine = BuildEngine();
        _yCalculator = new NodePositionYCalculator(Options.Create(new SchedulingConfiguration()));
    }

    /// <summary>Baseline: without the empty compound task the vertical order is correct (expected GREEN).</summary>
    [Fact]
    public async Task VerticalOrder_WithoutEmptyCompoundTask_IsCorrect()
    {
        await AssertVerticalOrderCorrect(false);
    }

    /// <summary>With the empty compound task the vertical order must stay correct (reproduces the defect).</summary>
    [Fact]
    public async Task VerticalOrder_WithEmptyCompoundTask_IsCorrect()
    {
        await AssertVerticalOrderCorrect(true);
    }

    private async Task AssertVerticalOrderCorrect(bool withEmptyCompoundTask)
    {
        // Stable IDs.
        var graspId = Guid.NewGuid();
        var emptyId = Guid.NewGuid();
        var jklId = Guid.NewGuid();
        var weldingId = Guid.NewGuid();
        var weldId = Guid.NewGuid();
        var holdId = Guid.NewGuid();
        var graspExec = Guid.NewGuid();
        var jklExec = Guid.NewGuid();

        // Top-level: sdf(Grasp), [dcd(empty)], jkl(Move), Welding(container: Weld, Hold)
        var grasp = Skill(graspId, "sdf", _graspSkill, 0, 45, graspExec);
        var jkl = Skill(jklId, "jkl", _moveSkill, 45, 65, jklExec);
        var welding = new TaskNode
        {
            ProcedureId = Guid.NewGuid(), Id = weldingId, Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Welding", StartTime = 110, Duration = 90 }
        };
        var weld = ChildSkill(weldId, "Weld Component", _weldSkill, weldingId, 110, 90);
        var hold = ChildSkill(holdId, "Hold Workpiece", _holdSkill, weldingId, 110, 90);

        var nodes = new List<Node> { grasp, jkl, welding, weld, hold };
        var edges = new List<DependencyEdge>
        {
            Fs(jklId, weldingId), // jkl -> Welding
            Edge(weldId, holdId, "left", "left"), // SS inside Welding
            Edge(holdId, weldId, "right", "right") // FF inside Welding
        };

        if (withEmptyCompoundTask)
        {
            // An empty COMPOUND task: the "dcd" container holds two empty child tasks (the two empty "sdf"
            // boxes in the screenshots), none of which resolve to executable work.
            var empty = new TaskNode
            {
                ProcedureId = Guid.NewGuid(), Id = emptyId, Position = new NodePosition { X = 0, Y = 0 },
                Task = new DomainTask { Name = "dcd", StartTime = 45, Duration = 0 }
            };
            var emptyChildA = new TaskNode
            {
                ProcedureId = Guid.NewGuid(), Id = Guid.NewGuid(), ParentId = emptyId,
                Position = new NodePosition { X = 0, Y = 0 },
                Task = new DomainTask { Name = "sdf", StartTime = 45, Duration = 0 }
            };
            var emptyChildB = new TaskNode
            {
                ProcedureId = Guid.NewGuid(), Id = Guid.NewGuid(), ParentId = emptyId,
                Position = new NodePosition { X = 0, Y = 0 },
                Task = new DomainTask { Name = "sdf", StartTime = 45, Duration = 0 }
            };
            nodes.Add(empty);
            nodes.Add(emptyChildA);
            nodes.Add(emptyChildB);
            edges.Add(Fs(graspId, emptyId)); // sdf -> dcd
            edges.Add(Fs(emptyId, jklId)); // dcd -> jkl
        }
        else
        {
            edges.Add(Fs(graspId, jklId)); // sdf -> jkl directly
        }

        // Execution state matching the screenshots: sdf completed, jkl running.
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [graspExec] = new()
            {
                ExecutionId = graspExec, SkillId = _graspSkill.Id, AgentId = _agent.Id,
                ActualStartTimeUtc = procStart, CurrentTimeIntoExecution = 45, EstimatedTotalDuration = 45,
                MinAchievableDuration = null, CompletedSuccessfully = true, Error = null, StatusMessage = ""
            },
            [jklExec] = new()
            {
                ExecutionId = jklExec, SkillId = _moveSkill.Id, AgentId = _agent.Id,
                ActualStartTimeUtc = procStart.AddSeconds(45), CurrentTimeIntoExecution = 20,
                EstimatedTotalDuration = 65, MinAchievableDuration = null, CompletedSuccessfully = false,
                Error = null, StatusMessage = ""
            }
        };

        var durationProvider = new ExecutionAwareDurationProvider(
            BuildPlanningProvider(), procStart, progress, Mock.Of<ILogger<ExecutionAwareDurationProvider>>());

        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(), CurrentTime = 65.0, StrictMode = false,
            IncludeDetailedTiming = true, DurationProvider = durationProvider
        };

        var result = await _timingCalculationEngine.CalculateTimingsAsync(ProcessNodeHierarchy(nodes), edges, options);
        Assert.True(result.Success, $"timing failed: {result.ErrorMessage}");
        Assert.NotNull(result.DetailedTimingInfo);

        // jkl and Welding must both be in the timing map (a node missing here sinks to the bottom in Y-sort).
        Assert.True(result.DetailedTimingInfo!.ContainsKey(jklId), "jkl missing from detailed timing");
        Assert.True(result.DetailedTimingInfo.ContainsKey(weldingId), "Welding missing from detailed timing");

        // Vertical positioning over the timing-applied nodes.
        var parentToChildren = result.UpdatedNodes
            .GroupBy(n => n.ParentId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Node>)g.ToList());
        var y = _yCalculator.CalculateYPositions(result.UpdatedNodes, parentToChildren, result.DetailedTimingInfo);

        var yGrasp = y[graspId];
        var yJkl = y[jklId];
        var yWelding = y[weldingId];

        // Correct vertical order: sdf above jkl above Welding (execution order).
        Assert.True(yGrasp < yJkl,
            $"sdf (Y={yGrasp}) should be above jkl (Y={yJkl})");
        Assert.True(yJkl < yWelding,
            $"jkl (Y={yJkl}) should be above Welding (Y={yWelding}) — jkl runs before Welding");
    }

    // ---- helpers ----

    private void SetupEstimate(Skill skill, double nominal)
    {
        _mockAgent.Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SkillExecutionEstimate
            {
                Skill = skill, AgentId = _agent.Id, CanExecuteAdaptively = false, EstimatedNominalDuration = nominal
            });
    }

    private SkillExecutionNode Skill(Guid id, string name, Skill skill, double start, double dur, Guid? exec)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(), Id = id, Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name, StartTime = start, Duration = dur, Skill = skill, AgentId = _agent.Id, ExecutionId = exec
            }
        };
    }

    private SkillExecutionNode ChildSkill(Guid id, string name, Skill skill, Guid parentId, double start, double dur)
    {
        var n = Skill(id, name, skill, start, dur, null);
        n.ParentId = parentId;
        return n;
    }

    private static DependencyEdge Fs(Guid s, Guid t)
    {
        return Edge(s, t, "right", "left");
    }

    private static DependencyEdge Edge(Guid s, Guid t, string sh, string th)
    {
        return new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(), Id = Guid.NewGuid(), SourceId = s, TargetId = t, SourceHandle = sh,
            TargetHandle = th
        };
    }

    private TimingCalculationEngine BuildEngine()
    {
        var edgeTypeMapper = new EdgeTypeMapper();
        var nodeResolver = new FHOOE.Freydis.Application.Services.Common.NodeResolver(
            Mock.Of<ILogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>>());
        var hierarchyProcessor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(Mock.Of<ILogger<NodeRelationshipMapper>>()),
            new HierarchyValidator(Mock.Of<ILogger<HierarchyValidator>>()),
            Mock.Of<ILogger<NodeHierarchyProcessor>>());
        var executionGraphBuilder = new ExecutionGraphBuilder(
            Mock.Of<ILogger<ExecutionGraphBuilder>>(), edgeTypeMapper, hierarchyProcessor, nodeResolver);
        var schedulePlanner = new SchedulePlanner(Mock.Of<ILogger<SchedulePlanner>>());

        var childNodeCollector = MockChildNodeCollector();
        var timingAggregator = MockTimingAggregator();
        var hierarchicalSorter = MockHierarchicalSorter();
        var taskNodeDurationCalculator = new TaskNodeDurationCalculator(
            childNodeCollector, timingAggregator, hierarchicalSorter, Mock.Of<ILogger<TaskNodeDurationCalculator>>());
        var routerNodeDurationCalculator = new RouterNodeDurationCalculator(
            Mock.Of<ILogger<RouterNodeDurationCalculator>>());
        var durationAdjuster = new NodeDurationAdjuster(
            taskNodeDurationCalculator, routerNodeDurationCalculator, childNodeCollector, timingAggregator,
            hierarchicalSorter, Mock.Of<ILogger<NodeDurationAdjuster>>());

        return new TimingCalculationEngine(
            executionGraphBuilder, schedulePlanner, taskNodeDurationCalculator, new TimingStatisticsCollector(),
            durationAdjuster, new NodeTimingMapper(Mock.Of<ILogger<NodeTimingMapper>>()), nodeResolver,
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

    private static IChildNodeCollector MockChildNodeCollector()
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

    private static ITimingAggregator MockTimingAggregator()
    {
        var mock = new Mock<ITimingAggregator>();
        mock.Setup(x => x.AggregateTimings(
                It.IsAny<IEnumerable<(double Duration, double StartTime, double FinishTime)>>()))
            .Returns((IEnumerable<(double Duration, double StartTime, double FinishTime)> timings) =>
            {
                var list = timings.ToList();
                if (list.Count == 0) return (0.0, 0.0, 0.0);
                return (list.Max(t => t.FinishTime) - list.Min(t => t.StartTime), list.Min(t => t.StartTime),
                    list.Max(t => t.FinishTime));
            });
        return mock.Object;
    }

    private static IHierarchicalSorter MockHierarchicalSorter()
    {
        var mock = new Mock<IHierarchicalSorter>();
        mock.Setup(x => x.SortTaskNodesHierarchically(It.IsAny<IReadOnlyList<TaskNode>>()))
            .Returns((IReadOnlyList<TaskNode> taskNodes) => taskNodes.ToList());
        return mock.Object;
    }
}