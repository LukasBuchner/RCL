using FHOOE.Freydis.Application.Services.Scheduling.Models;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Tests.TestUtilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     End-to-end timing tests for empty containers. An empty router branch (a TaskNode with no
///     executable descendants) must collapse to a zero-extent point at the router's resolved start
///     rather than claim its nominal duration at time 0, in every selection mode. Assertions read the
///     applied <c>Duration</c>/<c>StartTime</c> on the updated nodes — the values the positioning stage
///     turns linearly into <c>Position.X</c> and <c>Width</c>.
/// </summary>
public class EmptyBranchCollapseTests : IDisposable
{
    private readonly ITimingCalculationOrchestrator _orchestrator;
    private readonly ServiceProvider _serviceProvider;

    public EmptyBranchCollapseTests()
    {
        var services = new ServiceCollection();
        TestServiceConfiguration.ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = _serviceProvider.GetRequiredService<ITimingCalculationOrchestrator>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task NoPredecessor_SelectedEmptyBranch_CollapsesToZeroExtentAtZero()
    {
        var routerId = Guid.NewGuid();
        var emptyBranchId = Guid.NewGuid();
        var workSkillId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateRouter(routerId, "Router", 0, [("Default", emptyBranchId)]),
            CreateEmptyTask(emptyBranchId, "Default Branch"),
            CreateSkill(workSkillId, "Work", 0, 50)
        };

        var result = await CalculateAsync(nodes, [], new Dictionary<Guid, Guid> { [routerId] = emptyBranchId });

        var (routerDuration, routerStart) = RouterTiming(result, routerId);
        routerDuration.Should().Be(0, "an empty selected branch carries no work");
        routerStart.Should().Be(0, "the router has no predecessor");

        var (branchDuration, branchStart) = TaskTiming(result, emptyBranchId);
        branchDuration.Should().Be(0, "the empty branch must not claim its nominal duration");
        branchStart.Should().Be(0);
    }

    [Fact]
    public async Task Predecessor_SelectedEmptyBranch_AnchorsAtPredecessorFinish_NotPinnedAtZero()
    {
        var predId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var emptyBranchId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateSkill(predId, "Pred", 0, 30),
            CreateRouter(routerId, "Router", 0, [("Default", emptyBranchId)]),
            CreateEmptyTask(emptyBranchId, "Default Branch")
        };
        var edges = new List<DependencyEdge> { Edge(predId, routerId) };

        var result = await CalculateAsync(nodes, edges, new Dictionary<Guid, Guid> { [routerId] = emptyBranchId });

        var (routerDuration, routerStart) = RouterTiming(result, routerId);
        routerDuration.Should().Be(0);
        routerStart.Should().Be(30, "the router starts when its predecessor finishes, not at time 0");

        var (branchDuration, branchStart) = TaskTiming(result, emptyBranchId);
        branchDuration.Should().Be(0);
        branchStart.Should().Be(30, "the empty branch follows the router's predecessor-driven start");
    }

    [Fact]
    public async Task NoSelection_OnlyBranchEmpty_CollapsesAndOverridesNominalFallback()
    {
        var routerId = Guid.NewGuid();
        var emptyBranchId = Guid.NewGuid();
        var workSkillId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateRouter(routerId, "Router", 0, [("Default", emptyBranchId)]),
            CreateEmptyTask(emptyBranchId, "Default Branch"),
            CreateSkill(workSkillId, "Work", 0, 50)
        };

        // No router selection: the no-branch-timing fallback would otherwise apply the nominal duration.
        var result = await CalculateAsync(nodes, [], null);

        var (routerDuration, _) = RouterTiming(result, routerId);
        routerDuration.Should().Be(0, "a router with no work collapses even with no branch selected");

        var (branchDuration, _) = TaskTiming(result, emptyBranchId);
        branchDuration.Should().Be(0);
    }

    [Fact]
    public async Task NoSelection_PartiallyEmpty_RouterSpansNonEmptyBranch_EmptyBranchCollapses()
    {
        var routerId = Guid.NewGuid();
        var emptyBranchId = Guid.NewGuid();
        var liveBranchId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateRouter(routerId, "Router", 0, [("Empty", emptyBranchId), ("Live", liveBranchId)]),
            CreateEmptyTask(emptyBranchId, "Empty Branch"),
            CreateSkill(liveBranchId, "Live", 0, 40)
        };

        var result = await CalculateAsync(nodes, [], null);

        var (routerDuration, _) = RouterTiming(result, routerId);
        routerDuration.Should().Be(40, "the router spans the non-empty branch and ignores the empty one");

        var (branchDuration, branchStart) = TaskTiming(result, emptyBranchId);
        branchDuration.Should().Be(0, "the empty branch collapses even when a sibling carries work");
        branchStart.Should().Be(0);
    }

    [Fact]
    public async Task EmptyStandaloneTask_HasZeroExtent()
    {
        var emptyTaskId = Guid.NewGuid();
        var workSkillId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateEmptyTask(emptyTaskId, "Empty Task"),
            CreateSkill(workSkillId, "Work", 0, 50)
        };

        var result = await CalculateAsync(nodes, [], null);

        var (duration, _) = TaskTiming(result, emptyTaskId);
        duration.Should().Be(0, "a childless container occupies no scheduled extent");
    }

    [Fact]
    public async Task StandaloneEmptyTask_BetweenSkills_AnchorsAtPredecessorFinish()
    {
        var skillAId = Guid.NewGuid();
        var emptyTaskId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateSkill(skillAId, "A", 0, 30),
            CreateEmptyTask(emptyTaskId, "Empty"),
            CreateSkill(skillBId, "B", 30, 20)
        };
        var edges = new List<DependencyEdge>
        {
            Edge(skillAId, emptyTaskId),
            Edge(emptyTaskId, skillBId)
        };

        var result = await CalculateAsync(nodes, edges, null);

        var (duration, start) = TaskTiming(result, emptyTaskId);
        duration.Should().Be(0, "a leafless task occupies no scheduled extent");
        start.Should().Be(30, "the empty task sits at the finish of its predecessor skill");
    }

    [Fact]
    public async Task StandaloneEmptyTask_NestedEmpty_BothAtSamePredecessorDrivenPoint()
    {
        var skillAId = Guid.NewGuid();
        var outerEmptyId = Guid.NewGuid();
        var innerEmptyId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateSkill(skillAId, "A", 0, 45),
            CreateEmptyTask(outerEmptyId, "Outer Empty"),
            CreateEmptyTask(innerEmptyId, "Inner Empty", outerEmptyId)
        };
        var edges = new List<DependencyEdge> { Edge(skillAId, outerEmptyId) };

        var result = await CalculateAsync(nodes, edges, null);

        var (outerDuration, outerStart) = TaskTiming(result, outerEmptyId);
        outerDuration.Should().Be(0);
        outerStart.Should().Be(45, "the outer empty task anchors at its predecessor's finish");

        var (innerDuration, innerStart) = TaskTiming(result, innerEmptyId);
        innerDuration.Should().Be(0);
        innerStart.Should().Be(45, "the nested empty collapses to the same logical slot as its parent");
    }

    [Fact]
    public async Task StandaloneEmptyTask_NoPredecessor_AtZero()
    {
        var emptyTaskId = Guid.NewGuid();
        var workSkillId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateEmptyTask(emptyTaskId, "Empty"),
            CreateSkill(workSkillId, "Work", 0, 50)
        };

        var result = await CalculateAsync(nodes, [], null);

        var (duration, start) = TaskTiming(result, emptyTaskId);
        duration.Should().Be(0);
        start.Should().Be(0, "an empty task with no positioned predecessor sits at time 0");
    }

    [Fact]
    public async Task StandaloneEmptyTask_StartToStartPredecessor_AnchorsAtPredecessorStart()
    {
        var skillId = Guid.NewGuid();
        var emptyTaskId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateSkill(skillId, "Pred", 10, 40),
            CreateEmptyTask(emptyTaskId, "Empty")
        };
        var edges = new List<DependencyEdge> { Edge(skillId, emptyTaskId, "left") };

        var result = await CalculateAsync(nodes, edges, null);

        var (duration, start) = TaskTiming(result, emptyTaskId);
        duration.Should().Be(0);
        start.Should().Be(10, "a start-to-start edge anchors the empty task at the predecessor's start");
    }

    [Fact]
    public async Task SelectedBranchContainingSkill_NotCollapsed_RouterInheritsExtent()
    {
        var routerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            CreateRouter(routerId, "Router", 0, [("Default", branchId)]),
            CreateEmptyTask(branchId, "Branch"), // a TaskNode that now contains a skill
            CreateSkill(skillId, "Welding", 0, 25, branchId)
        };

        var result = await CalculateAsync(nodes, [], new Dictionary<Guid, Guid> { [routerId] = branchId });

        var (branchDuration, _) = TaskTiming(result, branchId);
        branchDuration.Should().Be(25, "a branch that carries a skill keeps that skill's extent");

        var (routerDuration, _) = RouterTiming(result, routerId);
        routerDuration.Should().Be(25, "the router inherits the timing of its non-empty selected branch");
    }

    private async Task<ScheduleResult> CalculateAsync(
        List<Node> nodes, List<DependencyEdge> edges, IReadOnlyDictionary<Guid, Guid>? routerSelections)
    {
        var request = new SchedulingRequest
        {
            ProcedureId = Guid.NewGuid(),
            Nodes = nodes,
            Edges = edges,
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = routerSelections
        };

        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();
        return result;
    }

    private static (double Duration, double StartTime) RouterTiming(ScheduleResult result, Guid id)
    {
        var router = (RouterNode)result.UpdatedNodes!.First(n => n.Id == id);
        return (router.RouterTask.Duration, router.RouterTask.StartTime);
    }

    private static (double Duration, double StartTime) TaskTiming(ScheduleResult result, Guid id)
    {
        var task = (TaskNode)result.UpdatedNodes!.First(n => n.Id == id);
        return (task.Task.Duration, task.Task.StartTime);
    }

    private static RouterNode CreateRouter(
        Guid id, string name, double startTime, IEnumerable<(string Name, Guid Target)> branches)
    {
        var branchList = branches.Select((b, i) => new ConditionalBranch
        {
            Name = b.Name,
            Condition = $"x == {i}",
            Priority = i + 1,
            TargetNodeId = b.Target
        }).ToList();

        return new RouterNode
        {
            Id = id,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = startTime,
                Duration = 200, // nominal: the collapse must override this to zero for an empty branch
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = branchList
            },
            ProcedureId = default
        };
    }

    private static TaskNode CreateEmptyTask(Guid id, string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask
            {
                Name = name,
                StartTime = 0,
                Duration = 200, // nominal: the collapse must override this to zero
                FinishTime = 200
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkill(
        Guid id, string name, double startTime, double duration, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = id,
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = startTime,
                Duration = duration,
                FinishTime = startTime + duration,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"{name} skill",
                    Properties = new List<TypedProperty>()
                }
            },
            ProcedureId = default
        };
    }

    private static DependencyEdge Edge(Guid source, Guid target, string? sourceHandle = null) => new()
    {
        Id = Guid.NewGuid(),
        SourceId = source,
        TargetId = target,
        SourceHandle = sourceHandle,
        ProcedureId = default
    };
}