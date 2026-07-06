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
///     Verifies that the timing engine plus vertical (Y) positioning lay a procedure out in execution order
///     regardless of an empty compound task on its chain. A representative multi-skill topology
///     (<c>sdf(Grasp) → [empty compound] → jkl(Move) → Welding{Weld SS/FF Hold} → fu(Inspect) →
///     router{branch / empty default} → sdf(Grasp)</c>) is run through <see cref="TimingCalculationEngine" />
///     and <see cref="NodePositionYCalculator" /> under execution progress. Each case is a controlled A/B on
///     the empty compound task; the shared nodes must keep the same vertical order whether or not the empty
///     container is present. This isolates the timing/positioning layer from the orchestration layer.
/// </summary>
public class EmptyCompoundTaskTimingEngineLayoutTests
{
    private readonly Dictionary<Guid, Mock<IRuntimeAgent>> _runtimeAgents = new();
    private readonly Dictionary<Guid, Agent> _agents = new();
    private readonly TimingCalculationEngine _engine;
    private readonly NodePositionYCalculator _yCalculator;

    // Skills (real, with real names as in the procedure).
    private readonly Skill _grasp = Sk("Grasp Object");
    private readonly Skill _move = Sk("Move Object To");
    private readonly Skill _weld = Sk("Weld");
    private readonly Skill _hold = Sk("Hold Position");
    private readonly Skill _inspect = Sk("Inspect Quality");

    // Agents (three, as in the export).
    private readonly Guid _agentMain = Guid.NewGuid(); // grasp(top)/move/weld/inspect/rg
    private readonly Guid _agentHold = Guid.NewGuid(); // hold
    private readonly Guid _agentBottom = Guid.NewGuid(); // grasp(bottom)

    public EmptyCompoundTaskTimingEngineLayoutTests()
    {
        SetupAgent(_agentMain, _grasp, _move, _weld, _inspect);
        SetupAgent(_agentHold, _hold);
        SetupAgent(_agentBottom, _grasp);
        _engine = BuildEngine();
        _yCalculator = new NodePositionYCalculator(Options.Create(new SchedulingConfiguration()));
    }

    [Fact]
    public async Task FullProcedure_WithoutEmptyCompoundTask_LaysOutInOrder()
    {
        await AssertLayoutInExecutionOrder(false);
    }

    [Fact]
    public async Task FullProcedure_WithEmptyCompoundTask_LaysOutInOrder()
    {
        await AssertLayoutInExecutionOrder(true);
    }

    private async Task AssertLayoutInExecutionOrder(bool withEmptyCompoundTask)
    {
        // ---- node ids ----
        var sdfTop = Guid.NewGuid();
        var dcd = Guid.NewGuid();
        var jkl = Guid.NewGuid();
        var welding = Guid.NewGuid();
        var weld = Guid.NewGuid();
        var hold = Guid.NewGuid();
        var fu = Guid.NewGuid();
        var fgh = Guid.NewGuid();
        var branch1 = Guid.NewGuid();
        var rg = Guid.NewGuid();
        var defaultBranch = Guid.NewGuid();
        var sdfBottom = Guid.NewGuid();
        var sdfTopExec = Guid.NewGuid();
        var jklExec = Guid.NewGuid();

        // ---- nodes ----
        var nodes = new List<Node>
        {
            SkillNode(sdfTop, "sdf", _grasp, _agentMain, 0, 45, sdfTopExec),
            SkillNode(jkl, "jkl", _move, _agentMain, 45, 65, jklExec),
            Container(welding, "Welding", 110, 90),
            ChildSkill(weld, "Weld Component", _weld, _agentMain, welding, 110, 90),
            ChildSkill(hold, "Hold Workpiece", _hold, _agentHold, welding, 110, 90),
            SkillNode(fu, "fu", _inspect, _agentMain, 200, 40, null),
            Router(fgh, "fgh", branch1, defaultBranch, 240, 65),
            Container(branch1, "Branch 1 Branch", 240, 65, fgh),
            ChildSkill(rg, "rg", _move, _agentMain, branch1, 240, 65),
            Container(defaultBranch, "Default Branch", 240, 0, fgh, true),
            SkillNode(sdfBottom, "sdf", _grasp, _agentBottom, 305, 55, null)
        };

        var edges = new List<DependencyEdge>
        {
            Fs(jkl, welding),
            Fs(welding, fu),
            Fs(fu, fgh),
            Fs(fgh, sdfBottom),
            Edge(weld, hold, "left", "left"), // SS
            Edge(hold, weld, "right", "right") // FF
        };

        if (withEmptyCompoundTask)
        {
            nodes.Add(Container(dcd, "dcd", 45, 0));
            edges.Add(Fs(sdfTop, dcd));
            edges.Add(Fs(dcd, jkl));
            edges.Add(Fs(sdfTop, jkl)); // direct edge present alongside, as in the live procedure
        }
        else
        {
            edges.Add(Fs(sdfTop, jkl));
        }

        // Execution state from the screenshots: sdf(top) completed, jkl running.
        var procStart = new DateTime(2026, 6, 30, 11, 0, 0, DateTimeKind.Utc);
        var progress = new Dictionary<Guid, SkillExecutionProgress>
        {
            [sdfTopExec] = Progress(sdfTopExec, _grasp.Id, _agentMain, procStart, 45, 45, true),
            [jklExec] = Progress(jklExec, _move.Id, _agentMain, procStart.AddSeconds(45), 20, 65, false)
        };

        var durationProvider = new ExecutionAwareDurationProvider(
            BuildPlanningProvider(), procStart, progress, Mock.Of<ILogger<ExecutionAwareDurationProvider>>());

        var options = new TimingCalculationOptions
        {
            ProcedureId = Guid.NewGuid(), CurrentTime = 65.0, StrictMode = false, IncludeDetailedTiming = true,
            DurationProvider = durationProvider,
            RouterSelections = new Dictionary<Guid, Guid> { [fgh] = branch1 }
        };

        var result = await _engine.CalculateTimingsAsync(ProcessNodeHierarchy(nodes), edges, options);

        // A silent fallback (empty detailed timing) is itself the defect — surface it explicitly.
        Assert.True(result.Success, $"timing calculation fell back / failed: {result.ErrorMessage}");
        Assert.NotNull(result.DetailedTimingInfo);
        Assert.NotEmpty(result.DetailedTimingInfo!);

        foreach (var (id, name) in new[]
                 {
                     (sdfTop, "sdf(top)"), (jkl, "jkl"), (welding, "Welding"), (fu, "fu"), (fgh, "fgh"),
                     (sdfBottom, "sdf(bottom)")
                 })
            Assert.True(result.DetailedTimingInfo!.ContainsKey(id),
                $"{name} missing from detailed timing -> it would sink to the bottom of the layout");

        // Vertical positioning over the timing-applied nodes.
        var parentToChildren = result.UpdatedNodes
            .GroupBy(n => n.ParentId ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Node>)g.ToList());
        var y = _yCalculator.CalculateYPositions(result.UpdatedNodes, parentToChildren, result.DetailedTimingInfo);

        // Top-level nodes must be laid out top-to-bottom in execution order.
        AssertAbove(y, sdfTop, jkl, "sdf(top)", "jkl");
        AssertAbove(y, jkl, welding, "jkl", "Welding");
        AssertAbove(y, welding, fu, "Welding", "fu");
        AssertAbove(y, fu, fgh, "fu", "fgh");
        AssertAbove(y, fgh, sdfBottom, "fgh", "sdf(bottom)");
    }

    private static void AssertAbove(IReadOnlyDictionary<Guid, double> y, Guid upper, Guid lower, string u, string l)
    {
        Assert.True(y.ContainsKey(upper) && y.ContainsKey(lower), $"missing Y for {u}/{l}");
        Assert.True(y[upper] < y[lower],
            $"{u} (Y={y[upper]}) must be above {l} (Y={y[lower]}) — execution order");
    }

    // ---- builders ----

    private static Skill Sk(string name)
    {
        return new Skill { Id = Guid.NewGuid(), Name = name, Description = name, Properties = [] };
    }

    private SkillExecutionNode SkillNode(Guid id, string name, Skill skill, Guid agentId, double start, double dur,
        Guid? exec)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(), Id = id, Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name, StartTime = start, Duration = dur, Skill = skill, AgentId = agentId, ExecutionId = exec
            }
        };
    }

    private SkillExecutionNode ChildSkill(Guid id, string name, Skill skill, Guid agentId, Guid parent, double start,
        double dur)
    {
        var n = SkillNode(id, name, skill, agentId, start, dur, null);
        n.ParentId = parent;
        return n;
    }

    private static TaskNode Container(Guid id, string name, double start, double dur, Guid? parent = null,
        bool hidden = false)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(), Id = id, ParentId = parent, Hidden = hidden,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = name, StartTime = start, Duration = dur }
        };
    }

    private static RouterNode Router(Guid id, string name, Guid branch1Target, Guid defaultTarget, double start,
        double dur)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(), Id = id, Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name, StartTime = start, Duration = dur, ManuallySelectedBranch = "Branch 1",
                Selector = new SimpleVariableSelector { Expression = "QualityOK" },
                Branches =
                [
                    new ConditionalBranch
                    {
                        Name = "Branch 1", Priority = 0, Condition = "QualityOK == true", TargetNodeId = branch1Target
                    },
                    new ConditionalBranch { Name = "Default", Priority = 999, TargetNodeId = defaultTarget }
                ]
            }
        };
    }

    private static SkillExecutionProgress Progress(Guid exec, Guid skillId, Guid agentId, DateTime start,
        double elapsed, double estTotal, bool completed)
    {
        return new SkillExecutionProgress
        {
            ExecutionId = exec, SkillId = skillId, AgentId = agentId, ActualStartTimeUtc = start,
            CurrentTimeIntoExecution = elapsed, EstimatedTotalDuration = estTotal, MinAchievableDuration = null,
            CompletedSuccessfully = completed, Error = null, StatusMessage = ""
        };
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

    private void SetupAgent(Guid agentId, params Skill[] skills)
    {
        if (!_agents.ContainsKey(agentId))
            _agents[agentId] = new Agent
            {
                Id = agentId, Name = "A" + agentId.ToString("N")[..4], SkillIds = skills.Select(s => s.Id).ToList(),
                RepresentativeColor = "Blue"
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

    private TimingCalculationEngine BuildEngine()
    {
        var nodeResolver = new FHOOE.Freydis.Application.Services.Common.NodeResolver(
            Mock.Of<ILogger<FHOOE.Freydis.Application.Services.Common.NodeResolver>>());
        var hierarchyProcessor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(Mock.Of<ILogger<NodeRelationshipMapper>>()),
            new HierarchyValidator(Mock.Of<ILogger<HierarchyValidator>>()),
            Mock.Of<ILogger<NodeHierarchyProcessor>>());
        var executionGraphBuilder = new ExecutionGraphBuilder(
            Mock.Of<ILogger<ExecutionGraphBuilder>>(), new EdgeTypeMapper(), hierarchyProcessor, nodeResolver);
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
        var agentService = new Mock<IAgentApplicationService>();
        agentService.Setup(s => s.GetAgentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _agents.GetValueOrDefault(id));
        var agentProvider = new Mock<IRuntimeAgentProvider>();
        agentProvider.Setup(p => p.GetRuntimeAgent(It.IsAny<Guid>()))
            .Returns((Guid id) => _runtimeAgents.TryGetValue(id, out var m) ? m.Object : null);
        var nodeAgentMapper = new NodeAgentMapper(
            agentProvider.Object, Mock.Of<ILogger<NodeAgentMapper>>(), agentService.Object);
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
                var c = allNodes.Where(n => n.ParentId == parentId).ToList();
                return (c.OfType<SkillExecutionNode>().ToList().AsReadOnly(),
                    c.OfType<TaskNode>().ToList().AsReadOnly(),
                    c.OfType<RouterNode>().ToList().AsReadOnly());
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