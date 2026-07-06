using System.Globalization;
using System.Reactive;
using System.Reactive.Subjects;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
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
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Perfolizer.Horology;
using Perfolizer.Metrology;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Benchmarks;

/// <summary>
///     BenchmarkDotNet configuration for end-to-end pipeline benchmarks.
/// </summary>
public class PipelineBenchmarkConfig : ManualConfig
{
    public PipelineBenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(15)
            .WithId("Pipeline"));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddExporter(MarkdownExporter.GitHub);

        var csvCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        csvCulture.NumberFormat.NumberGroupSeparator = string.Empty;

        var csvStyle = new SummaryStyle(
            csvCulture,
            true,
            printUnitsInContent: false,
            sizeUnit: SizeUnit.KB,
            timeUnit: TimeUnit.Microsecond,
            ratioStyle: RatioStyle.Trend);
        AddExporter(new CsvExporter(CsvSeparator.Comma, csvStyle));

        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));
    }
}

/// <summary>
///     End-to-end benchmarks for the full scheduling pipeline using the Application layer.
///     Uses REAL implementations for all services except IRouterBranchFilterService and INodeHidingService.
/// </summary>
[Config(typeof(PipelineBenchmarkConfig))]
[MemoryDiagnoser]
public class EndToEndPipelineBenchmarks
{
    private Dictionary<Guid, BenchmarkRuntimeAgent> _agents = null!;
    private TimingCalculationOrchestrator _orchestrator = null!;
    private Guid _procedureId;
    private SchedulingRequest _request = null!;

    [Params(40, 100, 200)] public int SkillCount { get; }

    [Params(15)] public int TaskCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _procedureId = Guid.NewGuid();

        // Create real agents for benchmark
        _agents = CreateBenchmarkAgents(5);

        // Create realistic node hierarchy with real agents
        var (nodes, edges) = CreateMixedComplexHierarchy(SkillCount, TaskCount, _procedureId, _agents);

        _request = new SchedulingRequest
        {
            ProcedureId = _procedureId,
            Nodes = nodes,
            Edges = edges,
            CurrentTime = 0,
            StrictMode = false,
            PreserveOriginalTaskDurations = false,
            IncludeDetailedTiming = true
        };

        _orchestrator = CreateOrchestrator(_agents);
    }

    [Benchmark(Description = "FullPipeline")]
    public async Task<ScheduleResult> FullPipeline()
    {
        return await _orchestrator.CalculateAsync(_request);
    }

    /// <summary>
    ///     Creates real benchmark agents with proper domain entities.
    /// </summary>
    private static Dictionary<Guid, BenchmarkRuntimeAgent> CreateBenchmarkAgents(int count)
    {
        var agents = new Dictionary<Guid, BenchmarkRuntimeAgent>();
        for (var i = 0; i < count; i++)
        {
            var agentId = Guid.NewGuid();
            var domainAgent = new Agent
            {
                Id = agentId,
                Name = $"BenchmarkAgent_{i + 1}",
                RepresentativeColor = $"#{i * 50 % 256:X2}{i * 80 % 256:X2}{i * 110 % 256:X2}"
            };
            agents[agentId] = new BenchmarkRuntimeAgent(domainAgent);
        }

        return agents;
    }

    /// <summary>
    ///     Creates the orchestrator with REAL Application layer implementations.
    ///     IRouterBranchFilterService and INodeHidingService are mocked (not relevant for benchmarks).
    /// </summary>
    private TimingCalculationOrchestrator CreateOrchestrator(
        Dictionary<Guid, BenchmarkRuntimeAgent> agents)
    {
        var schedulingConfig = Options.Create(new SchedulingConfiguration
        {
            Positioning = new PositioningConfiguration
            {
                TimeToPixelScale = 10.0,
                BaseYOffset = 50.0,
                SiblingSpacing = 60.0,
                ContainerTopPadding = 30.0,
                ContainerBottomPadding = 10.0,
                BaseHeight = 50.0,
                RouterDropdownHeight = 26.0
            },
            Defaults = new DefaultsConfiguration
            {
                DefaultTaskDuration = 200.0
            }
        });

        // ===== REAL Application layer implementations =====

        // Agent services for duration provider - REAL with benchmark agents
        var agentApplicationService = new BenchmarkAgentApplicationService(agents);
        var mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        mockAgentProvider.Setup(p => p.GetRuntimeAgent(It.IsAny<Guid>()))
            .Returns((Guid id) => agents.Values.Cast<IRuntimeAgent>().FirstOrDefault(a => a.Id == id));
        var nodeAgentMapper = new NodeAgentMapper(
            mockAgentProvider.Object,
            NullLogger<NodeAgentMapper>.Instance,
            agentApplicationService);
        var agentCapabilityAnalyzer = new AgentCapabilityAnalyzer(
            NullLogger<AgentCapabilityAnalyzer>.Instance);

        // Duration provider - REAL
        var planningModeDurationProvider = new PlanningModeDurationProvider(
            nodeAgentMapper,
            agentCapabilityAnalyzer,
            NullLogger<PlanningModeDurationProvider>.Instance);
        var durationProviderFactory = new DurationProviderFactory(
            planningModeDurationProvider,
            NullLogger<ExecutionAwareDurationProvider>.Instance);

        // Phase logger (no-op for benchmarks - reduces noise)
        var phaseLogger = new BenchmarkSchedulingPhaseLogger();

        // ===== REAL scheduling services =====

        // Hierarchy processing
        var nodeRelationshipMapper = new NodeRelationshipMapper(NullLogger<NodeRelationshipMapper>.Instance);
        var hierarchyValidator = new HierarchyValidator(NullLogger<HierarchyValidator>.Instance);
        var nodeHierarchyProcessor = new NodeHierarchyProcessor(
            nodeRelationshipMapper, hierarchyValidator, NullLogger<NodeHierarchyProcessor>.Instance);

        // Graph building
        var edgeTypeMapper = new EdgeTypeMapper();
        var executionGraphBuilder = new ExecutionGraphBuilder(
            NullLogger<ExecutionGraphBuilder>.Instance, edgeTypeMapper,
            nodeHierarchyProcessor,
            new Services.Common.NodeResolver(
                NullLogger<Services.Common.NodeResolver>.Instance));

        // Schedule planning
        var schedulePlanner = new SchedulePlanner(NullLogger<SchedulePlanner>.Instance);

        // Timing calculation helpers
        var childNodeCollector = new ChildNodeCollector(NullLogger<ChildNodeCollector>.Instance);
        var timingAggregator = new TimingAggregator(NullLogger<TimingAggregator>.Instance);
        var hierarchicalSorter = new HierarchicalSorter(NullLogger<HierarchicalSorter>.Instance);

        var taskNodeDurationCalculator = new TaskNodeDurationCalculator(
            childNodeCollector, timingAggregator, hierarchicalSorter,
            NullLogger<TaskNodeDurationCalculator>.Instance);

        var routerNodeDurationCalculator = new RouterNodeDurationCalculator(
            NullLogger<RouterNodeDurationCalculator>.Instance);

        var timingStatisticsCollector = new TimingStatisticsCollector();

        var nodeDurationAdjuster = new NodeDurationAdjuster(
            taskNodeDurationCalculator, routerNodeDurationCalculator,
            childNodeCollector, timingAggregator, hierarchicalSorter,
            NullLogger<NodeDurationAdjuster>.Instance);

        var nodeTimingMapper = new NodeTimingMapper(NullLogger<NodeTimingMapper>.Instance);

        // Main timing calculation engine
        var timingCalculationEngine = new TimingCalculationEngine(
            executionGraphBuilder, schedulePlanner, taskNodeDurationCalculator,
            timingStatisticsCollector, nodeDurationAdjuster, nodeTimingMapper,
            new Services.Common.NodeResolver(
                NullLogger<Services.Common.NodeResolver>.Instance),
            NullLogger<TimingCalculationEngine>.Instance);

        // Result conversion
        var scheduleResultConverter = new ScheduleResultConverter(NullLogger<ScheduleResultConverter>.Instance);

        // Positioning
        var positionXCalculator = new NodePositionXCalculator(schedulingConfig);
        var positionYCalculator = new NodePositionYCalculator(schedulingConfig);
        var nodeHeightCalculator = new NodeHeightCalculator(
            positionYCalculator, NullLogger<NodeHeightCalculator>.Instance);
        var nodeWidthCalculator = new NodeWidthCalculator(
            schedulingConfig, NullLogger<NodeWidthCalculator>.Instance);
        var nodePositioningService = new NodePositioningService(
            positionXCalculator, positionYCalculator, nodeHeightCalculator,
            nodeWidthCalculator, NullLogger<NodePositioningService>.Instance);

        // Analytics
        var timingAnalyzer = new TimingAnalyzer(NullLogger<TimingAnalyzer>.Instance);

        return new TimingCalculationOrchestrator(
            nodeHierarchyProcessor,
            timingCalculationEngine,
            scheduleResultConverter,
            durationProviderFactory,
            nodePositioningService,
            timingAnalyzer,
            phaseLogger,
            new Mock<IRouterBranchFilterService>().Object,
            new Mock<INodeHidingService>().Object,
            NullLogger<TimingCalculationOrchestrator>.Instance);
    }

    /// <summary>
    ///     Creates test nodes with real agents assigned.
    /// </summary>
    private static (IReadOnlyList<Node> Nodes, IReadOnlyList<DependencyEdge> Edges) CreateMixedComplexHierarchy(
        int skillCount, int taskCount, Guid procedureId,
        Dictionary<Guid, BenchmarkRuntimeAgent> agents)
    {
        var nodes = new List<Node>();
        var edges = new List<DependencyEdge>();
        var random = new Random(42);
        var agentList = agents.Values.ToList();
        var skillsPerTask = skillCount / taskCount;
        var remainingSkills = skillCount % taskCount;
        var skillId = 0;
        TaskNode? previousTask = null;

        for (var taskIndex = 0; taskIndex < taskCount; taskIndex++)
        {
            var taskId = Guid.NewGuid();
            var taskNode = new TaskNode
            {
                Id = taskId,
                ProcedureId = procedureId,
                Position = new NodePosition { X = 0, Y = taskIndex * 150 },
                Task = new Task
                {
                    Name = $"Task_{taskIndex + 1}",
                    Description = $"Task container {taskIndex + 1}",
                    StartTime = 0,
                    Duration = 100
                }
            };
            nodes.Add(taskNode);

            if (previousTask != null)
                edges.Add(new DependencyEdge
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = procedureId,
                    SourceId = previousTask.Id,
                    TargetId = taskId,
                    SourceHandle = "right",
                    TargetHandle = "left"
                });

            var skillsInThisTask = skillsPerTask + (taskIndex < remainingSkills ? 1 : 0);
            SkillExecutionNode? previousSkill = null;

            for (var i = 0; i < skillsInThisTask; i++)
            {
                var baseDuration = 10.0 + random.Next(50);
                var agent = agentList[random.Next(agentList.Count)];
                var skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = $"Skill_{++skillId}",
                    Description = $"Test skill {skillId}",
                    Properties = []
                };

                // Register skill with agent
                agent.RegisterSkill(skill);

                var skillNode = new SkillExecutionNode
                {
                    Id = Guid.NewGuid(),
                    ProcedureId = procedureId,
                    ParentId = taskId,
                    Position = new NodePosition { X = 20, Y = i * 60 },
                    SkillExecutionTask = new SkillExecutionTask
                    {
                        Name = skill.Name,
                        Description = skill.Description,
                        StartTime = 0,
                        Duration = baseDuration,
                        Skill = skill,
                        AgentId = agent.DomainAgent.Id
                    }
                };
                nodes.Add(skillNode);

                if (previousSkill != null && random.NextDouble() < 0.5)
                    edges.Add(new DependencyEdge
                    {
                        Id = Guid.NewGuid(),
                        ProcedureId = procedureId,
                        SourceId = previousSkill.Id,
                        TargetId = skillNode.Id,
                        SourceHandle = "right",
                        TargetHandle = "left"
                    });

                previousSkill = skillNode;
            }

            previousTask = taskNode;
        }

        return (nodes.AsReadOnly(), edges.AsReadOnly());
    }
}

#region Benchmark Support Implementations

/// <summary>
///     Real IRuntimeAgent implementation for benchmarking.
///     Provides agent capabilities without actual execution.
/// </summary>
public sealed class BenchmarkRuntimeAgent(Agent domainAgent) : IRuntimeAgent
{
    private readonly List<Skill> _skills = [];

    public Agent DomainAgent { get; } = domainAgent;
    public Guid Id { get; } = domainAgent.Id;
    public string Name { get; } = domainAgent.Name;

    public Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(new AgentHealthStatus
        {
            AgentId = Id,
            AgentName = Name,
            IsHealthy = true,
            IsAvailable = true,
            ActiveExecutions = 0,
            TotalExecutionsCompleted = 0,
            FailedExecutions = 0,
            LastSeenUtc = DateTime.UtcNow,
            StartedUtc = DateTime.UtcNow.AddHours(-1)
        });
    }

    public Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult<IReadOnlyList<Skill>>(_skills.AsReadOnly());
    }

    public Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill,
        CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult<SkillExecutionEstimate?>(new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = Id,
            CanExecuteAdaptively = false,
            EstimatedNominalDuration = 30.0
        });
    }

    public Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        return System.Threading.Tasks.Task.FromResult(false);
    }

    public IObservable<SkillExecutionProgress> ExecuteSkillAsync(
        Guid executionId, Skill skillToExecute, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Not needed for scheduling benchmarks");
    }

    public IObservable<SkillExecutionProgress> ExecuteSkillAdaptivelyAsync(
        Guid executionId, Skill skillToExecute, double initialTargetDuration,
        IObservable<double> plannedFinishTimes, IObservable<Unit> finishSignal,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Not needed for scheduling benchmarks");
    }

    public void RegisterSkill(Skill skill)
    {
        _skills.Add(skill);
    }
}

/// <summary>
///     IAgentApplicationService implementation for benchmarking.
///     Provides agent data from benchmark agents.
/// </summary>
public sealed class BenchmarkAgentApplicationService : IAgentApplicationService
{
    private readonly Dictionary<Guid, BenchmarkRuntimeAgent> _agents;
    private readonly Subject<IReadOnlyList<Agent>> _agentsChanged = new();

    public BenchmarkAgentApplicationService(Dictionary<Guid, BenchmarkRuntimeAgent> agents)
    {
        _agents = agents;
    }

    public Task<bool> DeleteAgentAsync(Guid agentId)
    {
        return System.Threading.Tasks.Task.FromResult(true);
    }

    public void Dispose()
    {
        _agentsChanged.Dispose();
    }

    public Task<Agent> CreateAgentAsync(Agent agent)
    {
        return System.Threading.Tasks.Task.FromResult(agent);
    }

    public Task<Agent?> UpdateAgentAsync(Agent agent)
    {
        return System.Threading.Tasks.Task.FromResult<Agent?>(agent);
    }

    public Task<IReadOnlyList<Agent>> GetAllAgentsAsync()
    {
        var domainAgents = _agents.Values.Select(a => a.DomainAgent).ToList();
        return System.Threading.Tasks.Task.FromResult<IReadOnlyList<Agent>>(domainAgents.AsReadOnly());
    }

    public Task<Agent?> GetAgentByIdAsync(Guid agentId)
    {
        var agent = _agents.TryGetValue(agentId, out var benchmarkAgent)
            ? benchmarkAgent.DomainAgent
            : null;
        return System.Threading.Tasks.Task.FromResult(agent);
    }

    public IObservable<IReadOnlyList<Agent>> OnAgentsChanged()
    {
        return _agentsChanged;
    }
}

/// <summary>
///     IRepository implementation for benchmarking (no-op - nodes/edges provided in request).
/// </summary>
public sealed class BenchmarkProcedureRepository : IRepository<Procedure>
{
    public Task<List<Procedure>> GetAllAsync()
    {
        return System.Threading.Tasks.Task.FromResult(new List<Procedure>());
    }

    public Task<Procedure?> GetByIdAsync(Guid id)
    {
        return System.Threading.Tasks.Task.FromResult<Procedure?>(null);
    }

    public Task<List<Procedure>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        return System.Threading.Tasks.Task.FromResult(new List<Procedure>());
    }

    public Task<Procedure> CreateAsync(Procedure entity)
    {
        return System.Threading.Tasks.Task.FromResult(entity);
    }

    public Task<bool> UpdateAsync(Procedure entity)
    {
        return System.Threading.Tasks.Task.FromResult(true);
    }

    public Task<bool> UpdateMultipleAsync(IReadOnlyList<Procedure> entities)
    {
        return System.Threading.Tasks.Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return System.Threading.Tasks.Task.FromResult(true);
    }
}

/// <summary>
///     ISchedulingPhaseLogger implementation for benchmarking (no-op to reduce noise).
/// </summary>
public sealed class BenchmarkSchedulingPhaseLogger : ISchedulingPhaseLogger
{
    public void LogPhaseStart(int phaseNumber, string phaseName, Guid procedureId)
    {
    }

    public void LogPhaseComplete(int phaseNumber, string phaseName, Guid procedureId, TimeSpan duration, string details)
    {
    }

    public void LogPipelineStart(Guid procedureId, int nodeCount, int edgeCount, bool strictMode, bool preserveOriginal,
        bool includeTiming)
    {
    }

    public void LogPipelineComplete(Guid procedureId, TimeSpan totalDuration, int scheduleCount,
        TimeSpan[] phaseDurations)
    {
    }

    public void LogTimingStatistics(Guid procedureId, TimingStatistics statistics)
    {
    }

    public void LogDetailedNodeTimings(Guid procedureId, IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo,
        IReadOnlyList<Node> nodes)
    {
    }

    public void LogCriticalPathAnalysis(Guid procedureId, CriticalPathInfo criticalPathInfo, IReadOnlyList<Node> nodes,
        IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo)
    {
    }
}

#endregion