using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.Filtering;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Timing;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Analytics;
using FHOOE.Freydis.Application.Services.UI.Visibility;
using FHOOE.Freydis.Application.Services.Variables;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using IPlannedSkillExecution = FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Tests.TestUtilities;

public static class TestServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register mocked dependencies for TimingCalculationOrchestrator
        var mockProcedureRepository = new Mock<IProcedureRepository>();
        mockProcedureRepository.Setup(r => r.GetAllNodesAsync()).ReturnsAsync(new List<Node>());
        mockProcedureRepository.Setup(r => r.GetAllEdgesAsync()).ReturnsAsync(new List<DependencyEdge>());
        var mockVariableResolver = new Mock<IVariableResolver>();
        var mockBranchSelector = new Mock<IBranchSelector>();

        services.AddSingleton(mockProcedureRepository.Object);
        services.AddSingleton<IRepository<Procedure>>(mockProcedureRepository.Object);
        services.AddSingleton(mockVariableResolver.Object);
        services.AddSingleton(mockBranchSelector.Object);

        // Register real implementations for processing services
        services.AddSingleton<IChildNodeCollector, ChildNodeCollector>();
        services.AddSingleton<ITimingAggregator, TimingAggregator>();
        services.AddSingleton<IHierarchicalSorter, HierarchicalSorter>();
        services.AddSingleton<IHierarchyValidator, HierarchyValidator>();
        services.AddSingleton<INodeDurationAdjuster, NodeDurationAdjuster>();
        services.AddSingleton<ITaskNodeDurationCalculator, TaskNodeDurationCalculator>();
        services.AddSingleton<IRouterNodeDurationCalculator, RouterNodeDurationCalculator>();
        services.AddSingleton<INodeRelationshipMapper, NodeRelationshipMapper>();
        services.AddSingleton<INodeTimingMapper, NodeTimingMapper>();
        services.AddSingleton<INodeHierarchyProcessor, NodeHierarchyProcessor>();
        services.AddSingleton<INodeResolver, NodeResolver>();
        services.AddSingleton<ITimingCalculationEngine, TimingCalculationEngine>();
        services.AddSingleton<IScheduleResultConverter, ScheduleResultConverter>();
        services.AddSingleton<ITimingStatisticsCollector, TimingStatisticsCollector>();
        services.AddSingleton<ITimingAnalyzer, TimingAnalyzer>();
        services.AddSingleton<ISchedulingPhaseLogger, SchedulingPhaseLogger>();

        // Register mock for ExecutionGraphBuilder
        var mockGraphBuilder = new Mock<IExecutionGraphBuilder>();
        mockGraphBuilder
            .Setup(b => b.BuildAsync(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyList<DependencyEdge>>(),
                It.IsAny<ISkillDurationProvider>(),
                It.IsAny<bool>()))
            .ReturnsAsync((IReadOnlyList<Node> nodes,
                IReadOnlyList<DependencyEdge> edges,
                ISkillDurationProvider provider,
                bool strictMode) =>
            {
                // Return empty execution graph for nodes, simulating successful build
                var skillNodes = nodes.OfType<SkillExecutionNode>().ToList();
                if (skillNodes.Count == 0)
                    return null;

                // Create a simple mock execution graph
                var mockGraph = new Mock<IExecutionGraph>();
                var mockExecutions = new List<IPlannedSkillExecution>();

                foreach (var skillNode in skillNodes)
                {
                    var mockExecution = new Mock<IPlannedSkillExecution>();
                    mockExecution.Setup(e => e.Id).Returns(skillNode.Id);
                    mockExecution.Setup(e => e.PlannedDuration).Returns(skillNode.SkillExecutionTask.Duration);
                    mockExecution.Setup(e => e.PlannedStartTime).Returns(skillNode.SkillExecutionTask.StartTime);
                    mockExecution.Setup(e => e.PlannedFinishTime).Returns(
                        skillNode.SkillExecutionTask.FinishTime ??
                        skillNode.SkillExecutionTask.StartTime + skillNode.SkillExecutionTask.Duration);
                    mockExecutions.Add(mockExecution.Object);
                }

                mockGraph.Setup(g => g.SkillExecutions).Returns(mockExecutions.AsReadOnly());
                mockGraph.Setup(g => g.Dependencies).Returns(new List<Dependency>().AsReadOnly());

                return mockGraph.Object;
            });
        services.AddSingleton(mockGraphBuilder.Object);

        // Register mock for SchedulePlanner
        var mockSchedulePlanner = new Mock<ISchedulePlanner>();
        mockSchedulePlanner
            .Setup(p => p.Plan(It.IsAny<IExecutionGraph>(), It.IsAny<double>()))
            .Returns(true);
        services.AddSingleton(mockSchedulePlanner.Object);

        // Register mock for DurationProviderFactory
        var mockDurationProviderFactory = new Mock<IDurationProviderFactory>();
        var mockDurationProvider = new Mock<ISkillDurationProvider>();

        mockDurationProviderFactory
            .Setup(x => x.CreateDurationProvider(It.IsAny<bool>(), It.IsAny<DateTime?>(),
                It.IsAny<IReadOnlyDictionary<Guid, SkillExecutionProgress>?>()))
            .Returns(mockDurationProvider.Object);
        services.AddSingleton(mockDurationProviderFactory.Object);

        // Register mock for NodePositioningService
        var mockNodePositioningService = new Mock<INodePositioningService>();
        mockNodePositioningService
            .Setup(s => s.ApplyPositionsAndHeights(
                It.IsAny<IReadOnlyList<Node>>(),
                It.IsAny<IReadOnlyDictionary<Guid, NodeTimingInfo>>(),
                It.IsAny<IReadOnlyDictionary<Guid, IReadOnlyList<Node>>>()))
            .Returns((IReadOnlyList<Node> nodes, IReadOnlyDictionary<Guid, NodeTimingInfo> timingInfo,
                IReadOnlyDictionary<Guid, IReadOnlyList<Node>> parentToChildren) => nodes);
        services.AddSingleton(mockNodePositioningService.Object);

        // Register loggers for all services that need them
        services.AddSingleton<ILogger<TimingCalculationOrchestrator>>(
            new TestLogger<TimingCalculationOrchestrator>());
        services.AddSingleton<ILogger<NodeHierarchyProcessor>>(
            new TestLogger<NodeHierarchyProcessor>());
        services.AddSingleton<ILogger<TimingCalculationEngine>>(
            new TestLogger<TimingCalculationEngine>());
        services.AddSingleton<ILogger<NodeResolver>>(
            new TestLogger<NodeResolver>());
        services.AddSingleton<ILogger<NodeRelationshipMapper>>(
            new TestLogger<NodeRelationshipMapper>());
        services.AddSingleton<ILogger<HierarchicalSorter>>(
            new TestLogger<HierarchicalSorter>());
        services.AddSingleton<ILogger<HierarchyValidator>>(
            new TestLogger<HierarchyValidator>());
        services.AddSingleton<ILogger<NodeTimingMapper>>(
            new TestLogger<NodeTimingMapper>());
        services.AddSingleton<ILogger<ChildNodeCollector>>(
            new TestLogger<ChildNodeCollector>());
        services.AddSingleton<ILogger<TimingAggregator>>(
            new TestLogger<TimingAggregator>());
        services.AddSingleton<ILogger<NodeDurationAdjuster>>(
            new TestLogger<NodeDurationAdjuster>());
        services.AddSingleton<ILogger<TaskNodeDurationCalculator>>(
            new TestLogger<TaskNodeDurationCalculator>());
        services.AddSingleton<ILogger<RouterNodeDurationCalculator>>(
            new TestLogger<RouterNodeDurationCalculator>());
        services.AddSingleton<ILogger<ScheduleResultConverter>>(
            new TestLogger<ScheduleResultConverter>());
        services.AddSingleton<ILogger<TimingStatisticsCollector>>(
            new TestLogger<TimingStatisticsCollector>());
        services.AddSingleton<ILogger<TimingAnalyzer>>(
            new TestLogger<TimingAnalyzer>());
        services.AddSingleton<ILogger<SchedulingPhaseLogger>>(
            new TestLogger<SchedulingPhaseLogger>());

        // Register real filter and hiding services (needed for Phase 0 in orchestrator)
        services.AddSingleton<IRouterBranchFilterService, RouterBranchFilterService>();
        services.AddSingleton<INodeHidingService, NodeHidingService>();
        services.AddSingleton<ILogger<RouterBranchFilterService>>(
            new TestLogger<RouterBranchFilterService>());
        services.AddSingleton<ILogger<NodeHidingService>>(
            new TestLogger<NodeHidingService>());

        // Register the orchestrator
        services.AddSingleton<ITimingCalculationOrchestrator, TimingCalculationOrchestrator>();
    }
}