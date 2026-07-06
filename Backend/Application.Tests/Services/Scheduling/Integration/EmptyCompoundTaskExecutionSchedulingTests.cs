using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.AgentCoordination.SkillMapping;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Integration;

/// <summary>
///     Verifies that inserting an empty compound task on a dependency chain does not corrupt the schedule
///     projected onto the timeline during execution. A skill graph is run through the REAL
///     <see cref="TimingCalculationOrchestrator" /> — router-branch filtering, hierarchy, timing, real X/Y
///     positioning, result conversion, and the hidden-node merge — with live execution progress. Each case is
///     a controlled A/B on the empty compound task; the shared visible nodes must schedule and lay out the
///     same whether or not the empty container is present. In particular, once a preceding skill has finished
///     and currentTime has advanced past it, the empty container's zero-extent placeholder must not drag that
///     finished successor forward to currentTime, each node's scheduled sort key must match its displayed
///     start, and the vertical lane order must follow execution order.
/// </summary>
public class EmptyCompoundTaskExecutionSchedulingTests
{
    private readonly Dictionary<Guid, Agent> _agents = new();
    private readonly Dictionary<Guid, Mock<IRuntimeAgent>> _runtimeAgents = new();

    private readonly Skill _grasp = Sk("Grasp Object");
    private readonly Skill _move = Sk("Move Object To");
    private readonly Skill _weld = Sk("Weld");
    private readonly Skill _hold = Sk("Hold Position");
    private readonly Skill _inspect = Sk("Inspect Quality");

    private readonly Guid _agentMain = Guid.NewGuid();
    private readonly Guid _agentHold = Guid.NewGuid();
    private readonly Guid _agentBottom = Guid.NewGuid();

    public EmptyCompoundTaskExecutionSchedulingTests()
    {
        SetupAgent(_agentMain, _grasp, _move, _weld, _inspect);
        SetupAgent(_agentHold, _hold);
        SetupAgent(_agentBottom, _grasp);
    }

    // Execution phases along the run, from the first skill running to deep into the Welding SS/FF ring.
    public enum Phase
    {
        JklRunning, // sdf done, jkl running (Welding not started)
        WeldRunningHoldNot, // sdf+jkl done, Weld running, Hold NOT started (partial SS/FF ring)
        WeldAndHoldRunning // sdf+jkl done, Weld+Hold both running
    }

    [Theory]
    [InlineData(false, Phase.JklRunning)]
    [InlineData(true, Phase.JklRunning)]
    [InlineData(false, Phase.WeldRunningHoldNot)]
    [InlineData(true, Phase.WeldRunningHoldNot)]
    [InlineData(false, Phase.WeldAndHoldRunning)]
    [InlineData(true, Phase.WeldAndHoldRunning)]
    public async Task FullPipeline_VisibleNodesInExecutionOrder(bool withEmptyCompoundTask, Phase phase)
    {
        await AssertVisibleTopLevelInExecutionOrder(withEmptyCompoundTask, phase);
    }

    private async Task AssertVisibleTopLevelInExecutionOrder(bool withEmptyCompoundTask, Phase phase)
    {
        var ids = new Ids();
        var (nodes, edges) = BuildProcedure(ids, withEmptyCompoundTask);

        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [ids.SdfTopExec] = Progress(ids.SdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, completed: true)
        };
        switch (phase)
        {
            case Phase.JklRunning:
                progress[ids.JklExec] = Progress(ids.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 20, 65,
                    completed: false);
                break;
            case Phase.WeldRunningHoldNot:
                progress[ids.JklExec] = Progress(ids.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 65, 65,
                    completed: true);
                progress[ids.WeldExec] = Progress(ids.WeldExec, _weld.Id, _agentMain, procStart.AddSeconds(110), 20,
                    90, completed: false);
                break;
            case Phase.WeldAndHoldRunning:
                progress[ids.JklExec] = Progress(ids.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 65, 65,
                    completed: true);
                progress[ids.WeldExec] = Progress(ids.WeldExec, _weld.Id, _agentMain, procStart.AddSeconds(110), 20,
                    90, completed: false);
                progress[ids.HoldExec] = Progress(ids.HoldExec, _hold.Id, _agentHold, procStart.AddSeconds(110), 20,
                    90, completed: false);
                break;
        }

        var currentTime = phase == Phase.JklRunning ? 65.0 : 130.0;
        var request = new SchedulingRequest
        {
            ProcedureId = Guid.NewGuid(),
            Nodes = nodes,
            Edges = edges,
            CurrentTime = currentTime,
            StrictMode = false,
            PreserveOriginalTaskDurations = false,
            IncludeDetailedTiming = true,
            ProcedureStartTimeUtc = procStart,
            ExecutionProgressData = progress,
            RouterSelections = new Dictionary<Guid, Guid> { [ids.Fgh] = ids.Branch1 }
        };

        var orchestrator = BuildRealOrchestrator();
        var result = await orchestrator.CalculateAsync(request);

        Assert.True(result.Success, $"pipeline failed/fell back: {result.ErrorMessage}");
        Assert.NotNull(result.UpdatedNodes);

        var byId = result.UpdatedNodes!.ToDictionary(n => n.Id);
        var sched = result.NodeSchedules.ToDictionary(s => s.NodeId);
        string SchedStr(Guid gid) => sched.TryGetValue(gid, out var s)
            ? $"abs={s.AbsoluteStartTime:F1},rel={s.RelativeStartTime:F1}"
            : "MISSING";

        // The visible top-level nodes, in the order they should appear on the timeline (execution order).
        var expected = new (Guid Id, string Name)[]
        {
            (ids.SdfTop, "sdf(top)"),
            (ids.Jkl, "jkl"),
            (ids.Welding, "Welding"),
            (ids.Fu, "fu"),
            (ids.Fgh, "fgh"),
            (ids.SdfBottom, "sdf(bottom)")
        };

        foreach (var (id, name) in expected)
        {
            Assert.True(byId.ContainsKey(id), $"{name} missing from emitted UpdatedNodes");
            Assert.False(byId[id].Hidden, $"{name} unexpectedly hidden");
        }

        // Assert each successive visible top-level node has a strictly greater Y (renders below the previous).
        for (var i = 1; i < expected.Length; i++)
        {
            var prev = byId[expected[i - 1].Id];
            var cur = byId[expected[i].Id];
            Assert.True(prev.Position.Y < cur.Position.Y,
                $"vertical order inverted: {expected[i - 1].Name} (Y={prev.Position.Y}) must be above " +
                $"{expected[i].Name} (Y={cur.Position.Y}). Nodes: " +
                string.Join(", ", expected.Select(e =>
                    $"{e.Name}[nodeStart={Start(byId[e.Id]):F1}, sched({SchedStr(e.Id)}), Y={byId[e.Id].Position.Y:F0}]")));
        }
    }

    private static double Start(Node n) => n switch
    {
        SkillExecutionNode s => s.SkillExecutionTask.StartTime,
        TaskNode t => t.Task.StartTime,
        RouterNode r => r.RouterTask.StartTime,
        _ => double.NaN
    };

    /// <summary>
    ///     Defect #2: the vertical-sort key (the schedule's absolute start, read from NodeSchedules /
    ///     detailedTimingInfo) must equal the start the node actually displays. When they diverge the timeline
    ///     sorts a node into one lane while showing another time. Red only with the empty compound task.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScheduleSortKey_MatchesDisplayedStart_DuringExecution(bool withEmptyCompoundTask)
    {
        var id = new Ids();
        var (nodes, edges) = BuildProcedure(id, withEmptyCompoundTask);
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [id.SdfTopExec] = Progress(id.SdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, completed: true),
            [id.JklExec] = Progress(id.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 65, 65,
                completed: true),
            [id.WeldExec] = Progress(id.WeldExec, _weld.Id, _agentMain, procStart.AddSeconds(110), 20, 90,
                completed: false)
        };

        var result = await RunAsync(nodes, edges, progress, 130.0,
            new Dictionary<Guid, Guid> { [id.Fgh] = id.Branch1 });
        Assert.True(result.Success, result.ErrorMessage);

        var byId = result.UpdatedNodes!.ToDictionary(n => n.Id);
        var sched = result.NodeSchedules.ToDictionary(s => s.NodeId);

        var displayed = Start(byId[id.Jkl]);
        var sortKey = sched[id.Jkl].AbsoluteStartTime;
        Assert.True(Math.Abs(displayed - sortKey) < 0.001,
            $"jkl's vertical-sort key ({sortKey}) diverges from its displayed start ({displayed}): " +
            "the timeline will place jkl by the sort key but render the displayed time.");
    }

    /// <summary>
    ///     Defect #1: a FINISHED node's scheduled start must stay at its actual start regardless of how far
    ///     currentTime has advanced. With the empty compound task the zero-extent placeholder is floored to
    ///     currentTime and drags the finished successor forward, so jkl's scheduled start tracks currentTime.
    /// </summary>
    [Theory]
    [InlineData(false, 130.0)]
    [InlineData(true, 130.0)]
    [InlineData(false, 250.0)]
    [InlineData(true, 250.0)]
    public async Task FinishedNodeScheduledStart_IndependentOfCurrentTime(bool withEmptyCompoundTask,
        double currentTime)
    {
        var id = new Ids();
        var (nodes, edges) = BuildProcedure(id, withEmptyCompoundTask);
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [id.SdfTopExec] = Progress(id.SdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, completed: true),
            [id.JklExec] = Progress(id.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 65, 65,
                completed: true)
        };

        var result = await RunAsync(nodes, edges, progress, currentTime,
            new Dictionary<Guid, Guid> { [id.Fgh] = id.Branch1 });
        Assert.True(result.Success, result.ErrorMessage);

        var sched = result.NodeSchedules.ToDictionary(s => s.NodeId);
        var jklStart = sched[id.Jkl].AbsoluteStartTime;
        Assert.True(Math.Abs(jklStart - 45.0) < 0.001,
            $"finished jkl's scheduled start is {jklStart}, expected 45 (its actual start); " +
            $"it was dragged toward currentTime={currentTime}.");
    }

    /// <summary>
    ///     Generality: the defect does not need the Welding SS/FF ring or the router. A minimal chain
    ///     <c>sdf → [asd(empty)] → jkl → qwe</c> with jkl finished and qwe running still inverts the vertical
    ///     order (jkl below qwe) only when the empty compound task is present.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MinimalChain_EmptyBetweenFinishedAndRunning_KeepsVerticalOrder(bool withEmptyCompoundTask)
    {
        var id = new Ids();
        var (nodes, edges) = BuildMinimalChain(id, withEmptyCompoundTask);
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [id.SdfTopExec] = Progress(id.SdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, completed: true),
            [id.JklExec] = Progress(id.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 65, 65,
                completed: true),
            [id.QweExec] = Progress(id.QweExec, _move.Id, _agentMain, procStart.AddSeconds(110), 20, 65,
                completed: false)
        };

        var result = await RunAsync(nodes, edges, progress, 130.0);
        Assert.True(result.Success, result.ErrorMessage);

        var byId = result.UpdatedNodes!.ToDictionary(n => n.Id);
        var sched = result.NodeSchedules.ToDictionary(s => s.NodeId);
        Assert.True(byId[id.Jkl].Position.Y < byId[id.Qwe].Position.Y,
            $"jkl (finished, Y={byId[id.Jkl].Position.Y}) must render above qwe (running, Y={byId[id.Qwe].Position.Y}); " +
            $"jkl schedule start={sched[id.Jkl].AbsoluteStartTime}, qwe schedule start={sched[id.Qwe].AbsoluteStartTime}.");
    }

    /// <summary>
    ///     When the running predecessor's live estimate changes, its not-started successor's start shifts to
    ///     track the predecessor's new estimated finish — correct execution-aware replanning. This asserts that
    ///     shift is identical whether or not the empty compound task is on the chain: the empty container is a
    ///     neutral zero-extent conduit that neither adds nor absorbs any of the movement.
    /// </summary>
    [Fact]
    public async Task EstimateChange_DownstreamShift_IdenticalWithAndWithoutEmptyCompoundTask()
    {
        async Task<double> ShiftAsync(bool withEmpty)
        {
            var id = new Ids();
            var (nodes, edges) = BuildMinimalChain(id, withEmpty);
            var low = await QweScheduledStartAsync(nodes, edges, id, jklEstimate: 65.0);
            var high = await QweScheduledStartAsync(nodes, edges, id, jklEstimate: 70.0);
            return high - low;
        }

        Assert.Equal(await ShiftAsync(withEmpty: false), await ShiftAsync(withEmpty: true), 3);
    }

    /// <summary>
    ///     Runs the minimal chain with sdf completed and jkl running at a given estimate, returning qwe's
    ///     scheduled absolute start.
    /// </summary>
    private async Task<double> QweScheduledStartAsync(
        List<Node> nodes, List<DependencyEdge> edges, Ids id, double jklEstimate,
        double elapsed = 20.0, double currentTime = 65.0)
    {
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [id.SdfTopExec] = Progress(id.SdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, completed: true),
            [id.JklExec] = Progress(id.JklExec, _move.Id, _agentMain, procStart.AddSeconds(45), elapsed, jklEstimate,
                completed: false)
        };

        var result = await RunAsync(nodes, edges, progress, currentTime);
        Assert.True(result.Success, result.ErrorMessage);
        return result.NodeSchedules.Single(s => s.NodeId == id.Qwe).AbsoluteStartTime;
    }

    private (List<Node> nodes, List<DependencyEdge> edges) BuildMinimalChain(Ids id, bool withEmpty)
    {
        var nodes = new List<Node>
        {
            SkillNode(id.SdfTop, "sdf", _grasp, _agentMain, 0, 45, id.SdfTopExec),
            SkillNode(id.Jkl, "jkl", _move, _agentMain, 45, 65, id.JklExec),
            SkillNode(id.Qwe, "qwe", _move, _agentMain, 110, 65, id.QweExec)
        };
        var edges = new List<DependencyEdge> { Fs(id.Jkl, id.Qwe) };

        if (withEmpty)
        {
            nodes.Add(Container(id.Dcd, "asd", 45, 0));
            edges.Add(Fs(id.SdfTop, id.Dcd));
            edges.Add(Fs(id.Dcd, id.Jkl));
            edges.Add(Fs(id.SdfTop, id.Jkl));
        }
        else
        {
            edges.Add(Fs(id.SdfTop, id.Jkl));
        }

        return (nodes, edges);
    }

    // ---------- procedure ----------

    private sealed class Ids
    {
        public readonly Guid SdfTop = Guid.NewGuid();
        public readonly Guid Dcd = Guid.NewGuid();
        public readonly Guid Jkl = Guid.NewGuid();
        public readonly Guid Welding = Guid.NewGuid();
        public readonly Guid Weld = Guid.NewGuid();
        public readonly Guid Hold = Guid.NewGuid();
        public readonly Guid Fu = Guid.NewGuid();
        public readonly Guid Fgh = Guid.NewGuid();
        public readonly Guid Branch1 = Guid.NewGuid();
        public readonly Guid Rg = Guid.NewGuid();
        public readonly Guid DefaultBranch = Guid.NewGuid();
        public readonly Guid SdfBottom = Guid.NewGuid();
        public readonly Guid SdfTopExec = Guid.NewGuid();
        public readonly Guid JklExec = Guid.NewGuid();
        public readonly Guid WeldExec = Guid.NewGuid();
        public readonly Guid HoldExec = Guid.NewGuid();

        // Minimal chain: sdf -> [asd(empty)] -> jkl -> qwe
        public readonly Guid Qwe = Guid.NewGuid();
        public readonly Guid QweExec = Guid.NewGuid();
    }

    /// <summary>Runs the request through the real full orchestrator.</summary>
    private async Task<ScheduleResult> RunAsync(
        List<Node> nodes, List<DependencyEdge> edges,
        Dictionary<Guid, SkillExecutionProgress> progress, double currentTime,
        Dictionary<Guid, Guid>? routerSelections = null)
    {
        var request = new SchedulingRequest
        {
            ProcedureId = Guid.NewGuid(),
            Nodes = nodes,
            Edges = edges,
            CurrentTime = currentTime,
            StrictMode = false,
            PreserveOriginalTaskDurations = false,
            IncludeDetailedTiming = true,
            ProcedureStartTimeUtc = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc),
            ExecutionProgressData = progress,
            RouterSelections = routerSelections
        };
        return await BuildRealOrchestrator().CalculateAsync(request);
    }

    private (List<Node> nodes, List<DependencyEdge> edges) BuildProcedure(Ids id, bool withEmpty)
    {
        var nodes = new List<Node>
        {
            SkillNode(id.SdfTop, "sdf", _grasp, _agentMain, 0, 45, id.SdfTopExec),
            SkillNode(id.Jkl, "jkl", _move, _agentMain, 45, 65, id.JklExec),
            Container(id.Welding, "Welding", 110, 90),
            ChildSkill(id.Weld, "Weld Component", _weld, _agentMain, id.Welding, 110, 90, id.WeldExec),
            ChildSkill(id.Hold, "Hold Workpiece", _hold, _agentHold, id.Welding, 110, 90, id.HoldExec),
            SkillNode(id.Fu, "fu", _inspect, _agentMain, 200, 40, null),
            Router(id.Fgh, "fgh", id.Branch1, id.DefaultBranch, 240, 65),
            Container(id.Branch1, "Branch 1 Branch", 240, 65, parent: id.Fgh),
            ChildSkill(id.Rg, "rg", _move, _agentMain, id.Branch1, 240, 65),
            Container(id.DefaultBranch, "Default Branch", 240, 0, parent: id.Fgh, hidden: true),
            SkillNode(id.SdfBottom, "sdf", _grasp, _agentBottom, 305, 55, null)
        };

        var edges = new List<DependencyEdge>
        {
            Fs(id.Jkl, id.Welding),
            Fs(id.Welding, id.Fu),
            Fs(id.Fu, id.Fgh),
            Fs(id.Fgh, id.SdfBottom),
            Edge(id.Weld, id.Hold, "left", "left"),
            Edge(id.Hold, id.Weld, "right", "right")
        };

        if (withEmpty)
        {
            nodes.Add(Container(id.Dcd, "dcd", 45, 0));
            edges.Add(Fs(id.SdfTop, id.Dcd));
            edges.Add(Fs(id.Dcd, id.Jkl));
            edges.Add(Fs(id.SdfTop, id.Jkl));
        }
        else
        {
            edges.Add(Fs(id.SdfTop, id.Jkl));
        }

        return (nodes, edges);
    }

    // ---------- real orchestrator (mirrors EndToEndPipelineBenchmarks wiring, with REAL filter + hiding) ----------

    private TimingCalculationOrchestrator BuildRealOrchestrator()
    {
        var config = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 10.0, BaseYOffset = 50.0, SiblingSpacing = 60.0, ContainerTopPadding = 30.0,
                ContainerBottomPadding = 10.0, BaseHeight = 50.0, RouterDropdownHeight = 26.0
            },
            Defaults = new DefaultsConfiguration { DefaultTaskDuration = 200.0 }
        });

        var agentService = new Mock<IAgentApplicationService>();
        agentService.Setup(s => s.GetAgentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid gid) => _agents.GetValueOrDefault(gid));
        var agentProvider = new Mock<IRuntimeAgentProvider>();
        agentProvider.Setup(p => p.GetRuntimeAgent(It.IsAny<Guid>()))
            .Returns((Guid gid) => _runtimeAgents.TryGetValue(gid, out var m) ? m.Object : null);

        var nodeAgentMapper = new NodeAgentMapper(
            agentProvider.Object, NullLogger<NodeAgentMapper>.Instance, agentService.Object);
        var agentCapabilityAnalyzer = new AgentCapabilityAnalyzer(NullLogger<AgentCapabilityAnalyzer>.Instance);

        var planningProvider = new PlanningModeDurationProvider(
            nodeAgentMapper, agentCapabilityAnalyzer, NullLogger<PlanningModeDurationProvider>.Instance);
        var durationProviderFactory = new DurationProviderFactory(
            planningProvider, NullLogger<ExecutionAwareDurationProvider>.Instance);

        var nodeRelationshipMapper = new NodeRelationshipMapper(NullLogger<NodeRelationshipMapper>.Instance);
        var hierarchyValidator = new HierarchyValidator(NullLogger<HierarchyValidator>.Instance);
        var nodeHierarchyProcessor = new NodeHierarchyProcessor(
            nodeRelationshipMapper, hierarchyValidator, NullLogger<NodeHierarchyProcessor>.Instance);

        var executionGraphBuilder = new ExecutionGraphBuilder(
            NullLogger<ExecutionGraphBuilder>.Instance, new EdgeTypeMapper(), nodeHierarchyProcessor,
            new FHOOE.Freydis.Application.Services.Common.NodeResolver(
                NullLogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>.Instance));
        var schedulePlanner = new SchedulePlanner(NullLogger<SchedulePlanner>.Instance);

        var childNodeCollector = new ChildNodeCollector(NullLogger<ChildNodeCollector>.Instance);
        var timingAggregator = new TimingAggregator(NullLogger<TimingAggregator>.Instance);
        var hierarchicalSorter = new HierarchicalSorter(NullLogger<HierarchicalSorter>.Instance);
        var taskNodeDurationCalculator = new TaskNodeDurationCalculator(
            childNodeCollector, timingAggregator, hierarchicalSorter,
            NullLogger<TaskNodeDurationCalculator>.Instance);
        var routerNodeDurationCalculator = new RouterNodeDurationCalculator(
            NullLogger<RouterNodeDurationCalculator>.Instance);
        var nodeDurationAdjuster = new NodeDurationAdjuster(
            taskNodeDurationCalculator, routerNodeDurationCalculator, childNodeCollector, timingAggregator,
            hierarchicalSorter, NullLogger<NodeDurationAdjuster>.Instance);
        var nodeTimingMapper = new NodeTimingMapper(NullLogger<NodeTimingMapper>.Instance);

        var timingCalculationEngine = new TimingCalculationEngine(
            executionGraphBuilder, schedulePlanner, taskNodeDurationCalculator, new TimingStatisticsCollector(),
            nodeDurationAdjuster, nodeTimingMapper,
            new FHOOE.Freydis.Application.Services.Common.NodeResolver(
                NullLogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>.Instance),
            NullLogger<TimingCalculationEngine>.Instance);

        var scheduleResultConverter = new ScheduleResultConverter(NullLogger<ScheduleResultConverter>.Instance);

        var positionXCalculator = new NodePositionXCalculator(config);
        var positionYCalculator = new NodePositionYCalculator(config);
        var nodeHeightCalculator = new NodeHeightCalculator(positionYCalculator,
            NullLogger<NodeHeightCalculator>.Instance);
        var nodeWidthCalculator = new NodeWidthCalculator(config, NullLogger<NodeWidthCalculator>.Instance);
        var nodePositioningService = new NodePositioningService(
            positionXCalculator, positionYCalculator, nodeHeightCalculator, nodeWidthCalculator,
            NullLogger<NodePositioningService>.Instance);

        var timingAnalyzer = new TimingAnalyzer(NullLogger<TimingAnalyzer>.Instance);

        // REAL router filter + hidden-node service (the layers under test).
        var routerBranchFilterService = new RouterBranchFilterService(
            NullLogger<RouterBranchFilterService>.Instance);
        var nodeHidingService = new NodeHidingService(NullLogger<NodeHidingService>.Instance);

        return new TimingCalculationOrchestrator(
            nodeHierarchyProcessor,
            timingCalculationEngine,
            scheduleResultConverter,
            durationProviderFactory,
            nodePositioningService,
            timingAnalyzer,
            new Mock<ISchedulingPhaseLogger>().Object,
            routerBranchFilterService,
            nodeHidingService,
            NullLogger<TimingCalculationOrchestrator>.Instance);
    }

    // ---------- builders ----------

    private static Skill Sk(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, Description = name, Properties = [] };

    private SkillExecutionNode SkillNode(Guid id, string name, Skill skill, Guid agentId, double start, double dur,
        Guid? exec) => new()
        {
            ProcedureId = Guid.NewGuid(), Id = id, Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name, StartTime = start, Duration = dur, Skill = skill, AgentId = agentId, ExecutionId = exec
            }
        };

    private SkillExecutionNode ChildSkill(Guid id, string name, Skill skill, Guid agentId, Guid parent, double start,
        double dur, Guid? exec = null)
    {
        var n = SkillNode(id, name, skill, agentId, start, dur, exec);
        n.ParentId = parent;
        return n;
    }

    private static TaskNode Container(Guid id, string name, double start, double dur, Guid? parent = null,
        bool hidden = false) => new()
        {
            ProcedureId = Guid.NewGuid(), Id = id, ParentId = parent, Hidden = hidden,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = name, StartTime = start, Duration = dur }
        };

    private static RouterNode Router(Guid id, string name, Guid branch1Target, Guid defaultTarget, double start,
        double dur) => new()
        {
            ProcedureId = Guid.NewGuid(), Id = id, Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name, StartTime = start, Duration = dur, ManuallySelectedBranch = "Branch 1",
                Selector = new SimpleVariableSelector { Expression = "QualityOK" },
                Branches =
            [
                new ConditionalBranch
                    { Name = "Branch 1", Priority = 0, Condition = "QualityOK == true", TargetNodeId = branch1Target },
                new ConditionalBranch { Name = "Default", Priority = 999, TargetNodeId = defaultTarget }
            ]
            }
        };

    private static SkillExecutionProgress Progress(Guid exec, Guid skillId, Guid agentId, DateTime start,
        double elapsed, double estTotal, bool completed) => new()
        {
            ExecutionId = exec, SkillId = skillId, AgentId = agentId, ActualStartTimeUtc = start,
            CurrentTimeIntoExecution = elapsed, EstimatedTotalDuration = estTotal, MinAchievableDuration = null,
            CompletedSuccessfully = completed, Error = null, StatusMessage = ""
        };

    private static DependencyEdge Fs(Guid s, Guid t) => Edge(s, t, "right", "left");

    private static DependencyEdge Edge(Guid s, Guid t, string sh, string th) => new()
    {
        ProcedureId = Guid.NewGuid(), Id = Guid.NewGuid(), SourceId = s, TargetId = t, SourceHandle = sh,
        TargetHandle = th
    };

    private void SetupAgent(Guid agentId, params Skill[] skills)
    {
        if (!_agents.ContainsKey(agentId))
            _agents[agentId] = new Agent
            {
                Id = agentId, Name = "A" + agentId.ToString("N")[..4],
                SkillIds = skills.Select(s => s.Id).ToList(), RepresentativeColor = "Blue"
            };
        else
            _agents[agentId].SkillIds = _agents[agentId].SkillIds.Concat(skills.Select(s => s.Id)).Distinct().ToList();

        if (!_runtimeAgents.TryGetValue(agentId, out var mock))
        {
            mock = new Mock<IRuntimeAgent>();
            mock.SetupGet(a => a.Id).Returns(agentId);
            mock.Setup(a => a.CanExecuteAdaptivelyAsync(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _runtimeAgents[agentId] = mock;
        }

        foreach (var skill in skills)
            mock.Setup(a => a.GetExecutionEstimateAsync(skill, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SkillExecutionEstimate
                {
                    Skill = skill, AgentId = agentId, CanExecuteAdaptively = false, EstimatedNominalDuration = 50.0
                });
    }
}