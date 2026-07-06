using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Duration;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Mapping;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Scheduling;
using FHOOE.Freydis.Scheduling.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using IPlannedSkillExecution =
    FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.IPlannedSkillExecution;
using ZeroExtentFiringPlaceholder =
    FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions.ZeroExtentFiringPlaceholder;
using CoreExec = FHOOE.Freydis.Scheduling.Core.IPlannedSkillExecution;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.GraphBuilding;

/// <summary>
///     Verifies that <see cref="ExecutionGraphBuilder" /> materializes a leafless container (an empty task,
///     or a router whose selected branch carries no executable work) as a zero-extent
///     <see cref="ZeroExtentFiringPlaceholder" /> rather than contracting it out. A chain
///     <c>A → empty → B</c> is carried through the rep as <c>A → empty• → B</c>, so the LP orders the
///     successor after the predecessor and the empty container has its own (zero-extent) LP node. The rep
///     is an LP ordering carrier only — it is never a real skill and is excluded from display/completion.
/// </summary>
public sealed class ExecutionGraphBuilderLeaflessMaterializationTests
{
    private readonly ExecutionGraphBuilder _builder;
    private readonly Mock<ISkillDurationProvider> _durationProvider = new();
    private readonly NodeHierarchyProcessor _hierarchyProcessor;
    private readonly NodeResolver _nodeResolver;

    public ExecutionGraphBuilderLeaflessMaterializationTests()
    {
        _durationProvider
            .Setup(d => d.AnalyzeAsync(It.IsAny<SkillExecutionNode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SkillExecutionNode n, CancellationToken _) =>
                Mock.Of<IPlannedSkillExecution>(p => p.Id == n.Id));

        _hierarchyProcessor = new NodeHierarchyProcessor(
            new NodeRelationshipMapper(NullLogger<NodeRelationshipMapper>.Instance),
            new HierarchyValidator(NullLogger<HierarchyValidator>.Instance),
            NullLogger<NodeHierarchyProcessor>.Instance);
        _nodeResolver = new NodeResolver(NullLogger<NodeResolver>.Instance);

        _builder = new ExecutionGraphBuilder(
            NullLogger<ExecutionGraphBuilder>.Instance,
            new EdgeTypeMapper(),
            _hierarchyProcessor,
            _nodeResolver);
    }

    [Fact]
    public async Task BuildAsync_EmptyTaskBetweenSkills_MaterializesZeroExtentPassthrough()
    {
        // A -> Task(empty) -> B materializes the empty task as a zero-extent rep: A -> empty• -> B.
        var skillA = CreateSkillNode("A");
        var emptyTask = CreateTaskNode();
        var skillB = CreateSkillNode("B");

        var graph = await _builder.BuildAsync(
            [skillA, emptyTask, skillB],
            [CreateFsEdge(skillA.Id, emptyTask.Id), CreateFsEdge(emptyTask.Id, skillB.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();

        // The empty container is materialized as a zero-extent rep alongside the two real skills.
        graph!.SkillExecutions.Select(e => e.Id)
            .Should().BeEquivalentTo([skillA.Id, emptyTask.Id, skillB.Id]);
        var rep = graph.SkillExecutions.Single(e => e.Id == emptyTask.Id);
        rep.Should().BeOfType<ZeroExtentFiringPlaceholder>();
        rep.PlannedDuration.Should().Be(0);

        // The chain is carried through the rep: A -> empty• -> B, both finish-to-start.
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id, d.Type))
            .Should().BeEquivalentTo(new[]
            {
                (skillA.Id, emptyTask.Id, DependencyType.FinishToStart),
                (emptyTask.Id, skillB.Id, DependencyType.FinishToStart)
            });
    }

    [Fact]
    public async Task BuildAsync_ChainOfEmptyTasks_MaterializesEachAndChains()
    {
        // A -> empty1 -> empty2 -> B materializes each empty: A -> e1• -> e2• -> B.
        var skillA = CreateSkillNode("A");
        var empty1 = CreateTaskNode();
        var empty2 = CreateTaskNode();
        var skillB = CreateSkillNode("B");

        var graph = await _builder.BuildAsync(
            [skillA, empty1, empty2, skillB],
            [
                CreateFsEdge(skillA.Id, empty1.Id),
                CreateFsEdge(empty1.Id, empty2.Id),
                CreateFsEdge(empty2.Id, skillB.Id)
            ],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Select(e => e.Id)
            .Should().Contain(new[] { empty1.Id, empty2.Id });
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id))
            .Should().BeEquivalentTo(new[]
            {
                (skillA.Id, empty1.Id),
                (empty1.Id, empty2.Id),
                (empty2.Id, skillB.Id)
            });
    }

    [Fact]
    public async Task BuildAsync_DeadEndEmptyTask_MaterializesWithIncomingOnly()
    {
        // A -> Task(empty) with no outgoing edge: the empty is still materialized and gated after A, but
        // gates nothing further.
        var skillA = CreateSkillNode("A");
        var emptyTask = CreateTaskNode();

        var graph = await _builder.BuildAsync(
            [skillA, emptyTask],
            [CreateFsEdge(skillA.Id, emptyTask.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Select(e => e.Id).Should().BeEquivalentTo([skillA.Id, emptyTask.Id]);
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id))
            .Should().BeEquivalentTo(new[] { (skillA.Id, emptyTask.Id) });
    }

    [Fact]
    public async Task BuildAsync_NonFsChainThroughEmpty_MaterializedEdgesKeepOwnHandles()
    {
        // A --FF--> Task(empty) --SS--> B. Materialization keeps each edge's own handles (unlike contraction's
        // splice), and the zero-extent rep makes the chain order B after A for these non-FS types too.
        var skillA = CreateSkillNode("A");
        var emptyTask = CreateTaskNode();
        var skillB = CreateSkillNode("B");

        var graph = await _builder.BuildAsync(
            [skillA, emptyTask, skillB],
            [
                CreateEdge(skillA.Id, emptyTask.Id, "right", "right"), // FF
                CreateEdge(emptyTask.Id, skillB.Id, "left", "left") // SS
            ],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.Dependencies.Select(d => (d.Source.Id, d.Target.Id, d.Type))
            .Should().BeEquivalentTo(new[]
            {
                (skillA.Id, emptyTask.Id, DependencyType.FinishToFinish),
                (emptyTask.Id, skillB.Id, DependencyType.StartToStart)
            });
    }

    [Fact]
    public async Task BuildAsync_EmptyBranchRouterBetweenSkills_MaterializesRouterPassthrough()
    {
        // Headline (procedure a63b7266): fu -> Router(empty selected branch) -> sdf. The router carries no
        // executable work, so it is materialized as a zero-extent rep: fu -> fgh• -> sdf orders sdf after fu.
        var fu = CreateSkillNode("fu");
        var router = CreateRouterNode("fgh");
        var emptyBranch = CreateChildTaskNode(router.Id);
        var sdf = CreateSkillNode("sdf");

        var graph = await _builder.BuildAsync(
            [fu, router, emptyBranch, sdf],
            [CreateFsEdge(fu.Id, router.Id), CreateFsEdge(router.Id, sdf.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Select(e => e.Id).Should().BeEquivalentTo([fu.Id, router.Id, sdf.Id]);
        graph.SkillExecutions.Single(e => e.Id == router.Id).Should().BeOfType<ZeroExtentFiringPlaceholder>();
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id, d.Type))
            .Should().BeEquivalentTo(new[]
            {
                (fu.Id, router.Id, DependencyType.FinishToStart),
                (router.Id, sdf.Id, DependencyType.FinishToStart)
            });
    }

    [Fact]
    public async Task BuildAsync_RouterWithExecutableBranch_NotMaterialized()
    {
        // A -> Router(branch with a real skill) -> B. The router carries executable work, so it is NOT
        // leafless and not materialized; the edges expand onto the branch skill (no router rep).
        var skillA = CreateSkillNode("A");
        var router = CreateRouterNode("R");
        var branchSkill = CreateChildSkillNode("branch", router.Id);
        var skillB = CreateSkillNode("B");

        var graph = await _builder.BuildAsync(
            [skillA, router, branchSkill, skillB],
            [CreateFsEdge(skillA.Id, router.Id), CreateFsEdge(router.Id, skillB.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Should().NotContain(e => e is ZeroExtentFiringPlaceholder);
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id))
            .Should().BeEquivalentTo(new[]
            {
                (skillA.Id, branchSkill.Id),
                (branchSkill.Id, skillB.Id)
            });
    }

    [Fact]
    public async Task BuildAsync_TaskWithOnlyUnanalyzableSkill_NotMaterialized()
    {
        // A -> Task[unanalyzable skill] -> B. The task's only child skill fails analysis, but the task still
        // resolves to that child structurally, so it is NOT leafless and is not materialized — no rep, no
        // spurious A -> B; the edges yield no dependency (non-strict) since the child produced no execution.
        var skillA = CreateSkillNode("A");
        var task = CreateTaskNode();
        var badSkill = CreateChildSkillNode("bad", task.Id);
        var skillB = CreateSkillNode("B");

        ReturnNullFor(badSkill.Id);

        var graph = await _builder.BuildAsync(
            [skillA, task, badSkill, skillB],
            [CreateFsEdge(skillA.Id, task.Id), CreateFsEdge(task.Id, skillB.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Select(e => e.Id).Should().BeEquivalentTo([skillA.Id, skillB.Id]);
        graph.SkillExecutions.Should().NotContain(e => e is ZeroExtentFiringPlaceholder);
        graph.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_StrictMode_TaskWithOnlyUnanalyzableSkill_Throws()
    {
        // In strict mode an edge endpoint that resolves to 0 executions aborts the build, naming the node.
        var skillA = CreateSkillNode("A");
        var task = CreateTaskNode();
        var badSkill = CreateChildSkillNode("bad", task.Id);

        ReturnNullFor(badSkill.Id);

        var act = async () => await _builder.BuildAsync(
            [skillA, task, badSkill],
            [CreateFsEdge(skillA.Id, task.Id)],
            _durationProvider.Object,
            true);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain(task.Id.ToString());
    }

    [Fact]
    public async Task BuildAsync_TaskWithOneGoodOneBadSkill_ExpandsOntoGoodSkill()
    {
        // A -> Task[good, bad] -> B. The good skill schedules; the task is not leafless, not materialized; its
        // edges expand onto the good skill: A -> good and good -> B.
        var skillA = CreateSkillNode("A");
        var task = CreateTaskNode();
        var goodSkill = CreateChildSkillNode("good", task.Id);
        var badSkill = CreateChildSkillNode("bad", task.Id);
        var skillB = CreateSkillNode("B");

        ReturnNullFor(badSkill.Id);

        var graph = await _builder.BuildAsync(
            [skillA, task, goodSkill, badSkill, skillB],
            [CreateFsEdge(skillA.Id, task.Id), CreateFsEdge(task.Id, skillB.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        graph!.SkillExecutions.Should().NotContain(e => e is ZeroExtentFiringPlaceholder);
        graph.Dependencies.Select(d => (d.Source.Id, d.Target.Id))
            .Should().BeEquivalentTo(new[]
            {
                (skillA.Id, goodSkill.Id),
                (goodSkill.Id, skillB.Id)
            });
    }

    [Fact]
    public async Task BuildAsync_CompletenessRegression_LeaflessChainEdgesAreMaterialized()
    {
        // Regression guard against the original bug (a dropped prerequisite let the successor fire early):
        // BOTH chain edges must be present in the materialized dependency set — a membership assertion, not
        // merely an ordering one. A future change that stops materializing would make this go red.
        var skillA = CreateSkillNode("A");
        var emptyTask = CreateTaskNode();
        var skillB = CreateSkillNode("B");

        var graph = await _builder.BuildAsync(
            [skillA, emptyTask, skillB],
            [CreateFsEdge(skillA.Id, emptyTask.Id), CreateFsEdge(emptyTask.Id, skillB.Id)],
            _durationProvider.Object);

        graph.Should().NotBeNull();
        var pairs = graph!.Dependencies.Select(d => (d.Source.Id, d.Target.Id)).ToList();
        pairs.Should().Contain((skillA.Id, emptyTask.Id));
        pairs.Should().Contain((emptyTask.Id, skillB.Id));
    }

    [Fact]
    public void PlanSchedule_PlaceholderOnCrossSccEdge_SolvesAndOrdersThroughZeroExtent()
    {
        // C -> P(empty•) -> {A,B} where A,B are a coupled SS/FF ring. The placeholder P sits on cross-SCC
        // condensation edges (C-SCC -> P -> {A,B}-SCC). The LP must key its internal-data over the Core-only
        // placeholder (no KeyNotFoundException), keep it zero-extent, and carry ordering through it: A starts
        // no earlier than C finishes.
        var c = new StubExec { Id = Guid.NewGuid(), PlannedDuration = 1 };
        var a = new StubExec { Id = Guid.NewGuid(), PlannedDuration = 1 };
        var b = new StubExec { Id = Guid.NewGuid(), PlannedDuration = 1 };
        var p = new ZeroExtentFiringPlaceholder { Id = Guid.NewGuid() };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<CoreExec> { c, a, b, p }.AsReadOnly(),
            Dependencies = new List<Dependency>
            {
                new() { Id = Guid.NewGuid(), Source = a, Target = b, Type = DependencyType.StartToStart },
                new() { Id = Guid.NewGuid(), Source = b, Target = a, Type = DependencyType.FinishToFinish },
                new() { Id = Guid.NewGuid(), Source = c, Target = p, Type = DependencyType.FinishToStart },
                new() { Id = Guid.NewGuid(), Source = p, Target = a, Type = DependencyType.FinishToStart }
            }.AsReadOnly()
        };

        var solved = graph.PlanSchedule(0);

        var sc = solved.SkillExecutions.Single(e => e.Id == c.Id);
        var sa = solved.SkillExecutions.Single(e => e.Id == a.Id);
        var sp = solved.SkillExecutions.Single(e => e.Id == p.Id);
        sp.PlannedDuration.Should().Be(0);
        sa.PlannedStartTime.Should().BeGreaterThanOrEqualTo(sc.PlannedFinishTime - 1e-9);
    }

    [Fact]
    public void LeaflessClassification_MatchesResolverForTaskWithOnlyUnanalyzableSkill()
    {
        // Cross-layer invariant: a task with only an unanalyzable skill is structurally non-leafless (the
        // resolver oracle the runtime/display use returns its child), so the builder must not materialize it.
        var task = CreateTaskNode();
        var badSkill = CreateChildSkillNode("bad", task.Id);

        var hierarchy = _hierarchyProcessor.ProcessHierarchy([task, badSkill]);

        _nodeResolver.ResolveToExecutableIds(task.Id, hierarchy).Any()
            .Should().BeTrue("the task resolves to its child skill, so it is structurally non-leafless");
        _nodeResolver.ResolveToExecutableIds(task.Id, hierarchy)
            .Should().Contain(badSkill.Id);
    }

    [Fact]
    public async Task BuildAsync_BranchEntrySourcedEdge_ThrowsEvenInNonStrict()
    {
        // Invariant: a router's branch-entry task has no outgoing dependency edge (a router's out-edges are
        // sourced on the router itself). A violation breaks the completeness of leafless materialization, so
        // the build fails loudly even in non-strict mode rather than letting the ordering escape the rep.
        var router = CreateRouterNode("R");
        var branchEntry = CreateChildTaskNode(router.Id);
        var skillB = CreateSkillNode("B");

        var act = async () => await _builder.BuildAsync(
            [router, branchEntry, skillB],
            [CreateFsEdge(branchEntry.Id, skillB.Id)],
            _durationProvider.Object);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain(branchEntry.Id.ToString());
    }

    [Fact]
    public void PlanSchedule_NonFsChainThroughEmpty_OrdersSuccessorAfterPredecessor()
    {
        // A --FF--> empty• --SS--> B through a zero-extent rep. FF pins F(empty) >= F(A); SS pins
        // S(B) >= S(empty); the rep is zero-extent so S(empty) = F(empty). Composed, B starts no earlier than A
        // finishes — the non-FS chain is genuinely time-ordered through the empty container, not merely present
        // as a dependency pair.
        var a = new StubExec { Id = Guid.NewGuid(), PlannedDuration = 2 };
        var empty = new ZeroExtentFiringPlaceholder { Id = Guid.NewGuid() };
        var b = new StubExec { Id = Guid.NewGuid(), PlannedDuration = 1 };

        var graph = new ExecutionGraph
        {
            SkillExecutions = new List<CoreExec> { a, empty, b }.AsReadOnly(),
            Dependencies = new List<Dependency>
            {
                new() { Id = Guid.NewGuid(), Source = a, Target = empty, Type = DependencyType.FinishToFinish },
                new() { Id = Guid.NewGuid(), Source = empty, Target = b, Type = DependencyType.StartToStart }
            }.AsReadOnly()
        };

        var solved = graph.PlanSchedule(0);

        var sa = solved.SkillExecutions.Single(e => e.Id == a.Id);
        var se = solved.SkillExecutions.Single(e => e.Id == empty.Id);
        var sb = solved.SkillExecutions.Single(e => e.Id == b.Id);
        se.PlannedDuration.Should().Be(0);
        sb.PlannedStartTime.Should().BeGreaterThanOrEqualTo(sa.PlannedFinishTime - 1e-9);
    }

    /// <summary>
    ///     Overrides the duration provider so analysis of the named skill node returns <c>null</c>,
    ///     modelling a skill whose planning analysis failed.
    /// </summary>
    /// <param name="skillId">The ID of the skill node whose analysis should fail.</param>
    private void ReturnNullFor(Guid skillId)
    {
        _durationProvider
            .Setup(d => d.AnalyzeAsync(It.Is<SkillExecutionNode>(n => n.Id == skillId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPlannedSkillExecution?)null);
    }

    private static SkillExecutionNode CreateChildSkillNode(string name, Guid parentId)
    {
        var node = CreateSkillNode(name);
        node.ParentId = parentId;
        return node;
    }

    private static SkillExecutionNode CreateSkillNode(string name)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0,
                Duration = 1,
                Skill = new Skill { Id = Guid.NewGuid(), Name = name, Description = "", Properties = [] },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static TaskNode CreateTaskNode()
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new DomainTask { Name = "Empty", StartTime = 0, Duration = 0 }
        };
    }

    private static TaskNode CreateChildTaskNode(Guid parentId)
    {
        var node = CreateTaskNode();
        node.ParentId = parentId;
        return node;
    }

    private static RouterNode CreateRouterNode(string name)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = name,
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Default", Priority = 1, TargetNodeId = Guid.NewGuid() }
                }
            }
        };
    }

    /// <summary>
    ///     A minimal Core-level execution stub with working get/set timing properties, used to drive the LP
    ///     directly. Not an <c>ISkillExecution</c>, so the LP treats it as a fixed-duration task.
    /// </summary>
    private sealed class StubExec : CoreExec
    {
        public Guid Id { get; init; }
        public double PlannedStartTime { get; set; }
        public double PlannedFinishTime { get; set; }
        public double PlannedDuration { get; set; }
    }

    private static DependencyEdge CreateFsEdge(Guid source, Guid target)
    {
        return CreateEdge(source, target, "right", "left");
    }

    private static DependencyEdge CreateEdge(Guid source, Guid target, string sourceHandle, string targetHandle)
    {
        return new DependencyEdge
        {
            ProcedureId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            SourceId = source,
            TargetId = target,
            SourceHandle = sourceHandle,
            TargetHandle = targetHandle
        };
    }
}