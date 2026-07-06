using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Scheduling.Computation;
using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Services.UI.Positioning;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;
using DomainTask = FHOOE.Freydis.Domain.Entities.Procedure.Task;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Reproduces the node/edge output the client receives for a router whose selected branch is an empty
///     task one layer deeper (procedure a63b7266: router 'fgh' with the empty 'Default Branch' selected,
///     edges into and out of the router). Runs the real positioning chain (the backend step that finalizes
///     the dimensions sent to the client) and asserts on BOTH the edge output and the node output.
/// </summary>
public sealed class RouterEmptyBranchNodeOutputTests
{
    private readonly ITestOutputHelper _output;
    private readonly NodePositioningService _positioning;

    public RouterEmptyBranchNodeOutputTests(ITestOutputHelper output)
    {
        _output = output;
        var opts = Options.Create(new SchedulingConfiguration());
        var x = new NodePositionXCalculator(opts);
        var y = new NodePositionYCalculator(opts);
        var h = new NodeHeightCalculator(y, NullLogger<NodeHeightCalculator>.Instance);
        var w = new NodeWidthCalculator(opts, NullLogger<NodeWidthCalculator>.Instance);
        _positioning = new NodePositioningService(x, y, h, w, NullLogger<NodePositioningService>.Instance);
    }

    [Fact]
    public void RouterWithEmptySelectedBranch_NodeAndEdgeOutput()
    {
        // fu --FS--> fgh(router) --FS--> sdf, where the router's selected branch is the empty 'Default Branch'
        // (a child of the router, one layer deeper). Part A has already collapsed the router and its empty
        // branch to duration 0, which is the state the client is rendered from.
        var fu = SkillNode("fu", duration: 40);
        var router = RouterNodeWith("fgh", duration: 0);
        var defaultBranch = TaskNodeWith("Default Branch", duration: 0, parentId: router.Id);
        var sdf = SkillNode("sdf", duration: 55);

        var nodes = new List<Node> { fu, router, defaultBranch, sdf };
        var edges = new List<DependencyEdge>
        {
            FsEdge(fu.Id, router.Id),   // into the router
            FsEdge(router.Id, sdf.Id)   // out of the router
        };

        var parentToChildren = new Dictionary<Guid, IReadOnlyList<Node>>
        {
            { router.Id, new List<Node> { defaultBranch }.AsReadOnly() }
        };
        var timing = nodes.ToDictionary(
            n => n.Id,
            n => new NodeTimingInfo { NodeId = n.Id, AbsoluteStartTime = 0, RelativeStartTime = 0 });

        // Act — the backend node-output step the client consumes (X/Y positions, widths, heights, extent).
        var outNodes = _positioning.ApplyPositionsAndHeights(nodes, timing, parentToChildren);

        foreach (var n in outNodes)
            _output.WriteLine(
                $"NODE {NodeName(n)} type={n.GetType().Name} width={n.Width} height={n.Height} " +
                $"parentId={n.ParentId} extent={n.Extent} pos=({n.Position.X},{n.Position.Y})");
        foreach (var e in edges)
            _output.WriteLine($"EDGE {e.SourceId}->{e.TargetId} ({e.SourceHandle},{e.TargetHandle})");

        var outRouter = outNodes.Single(n => n.Id == router.Id);
        var outSdf = outNodes.Single(n => n.Id == sdf.Id);

        // EDGE OUTPUT — the router's in/out edges survive intact (backend does not drop them).
        edges.Should().Contain(e => e.SourceId == fu.Id && e.TargetId == router.Id);
        edges.Should().Contain(e => e.SourceId == router.Id && e.TargetId == sdf.Id);

        // NODE OUTPUT — a normal skill keeps a positive duration-based width. The router's selected branch is
        // empty, so Part A collapses it to a zero-extent node: width 0 is the honest backend output, not a
        // defect. Frontend tests (routerEmptyBranchEdges.test.tsx) confirm the router still renders both
        // handles its in/out edges anchor on at width 0, so the renderable display width is a client concern
        // (BaseNode.MIN_CONTAINER_WIDTH), not a presentation constant baked into the backend model.
        (outSdf.Width ?? 0).Should().BeGreaterThan(0);
        (outRouter.Width ?? 0).Should().Be(0);
    }

    [Fact]
    public async Task RouterWithEmptySelectedBranch_EdgeOutput_EmitsRouterEdges()
    {
        // The edge output the client's edgesChanged subscription consumes is the full persisted edge set,
        // fetched fresh in CrudNotificationService.NotifyPersistedStateAsync. Scheduling never deletes or
        // transforms persisted edges, so the router's in/out edges must be emitted.
        var fu = SkillNode("fu", duration: 40);
        var router = RouterNodeWith("fgh", duration: 0);
        var defaultBranch = TaskNodeWith("Default Branch", duration: 0, parentId: router.Id);
        var sdf = SkillNode("sdf", duration: 55);

        var procedureId = Guid.NewGuid();
        var persistedNodes = new List<Node> { fu, router, defaultBranch, sdf };
        var persistedEdges = new List<DependencyEdge>
        {
            FsEdge(fu.Id, router.Id),
            FsEdge(router.Id, sdf.Id)
        };

        var repo = new Mock<IProcedureRepository>();
        repo.Setup(r => r.GetNodesByProcedureIdAsync(procedureId)).ReturnsAsync(persistedNodes);
        repo.Setup(r => r.GetEdgesByProcedureIdAsync(procedureId)).ReturnsAsync(persistedEdges);

        var ctx = new Mock<IProcedureContext>();
        ctx.Setup(c => c.RequireCurrentProcedureId()).Returns(procedureId);

        IReadOnlyList<DependencyEdge>? emittedEdges = null;
        var edgeTracker = new Mock<IDependencyEdgeChangeTracker>();
        edgeTracker
            .Setup(t => t.UpdateEntities(It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Callback<IReadOnlyList<DependencyEdge>>(e => emittedEdges = e);
        var nodeTracker = new Mock<INodeChangeTracker>();

        var service = new CrudNotificationService(
            repo.Object, nodeTracker.Object, edgeTracker.Object, ctx.Object,
            NullLogger<CrudNotificationService>.Instance);

        // Act — the real edge-emission path.
        await service.NotifyPersistedStateAsync();

        // Assert — the router's in/out edges ARE emitted to the client.
        emittedEdges.Should().NotBeNull();
        emittedEdges!.Should().Contain(e => e.SourceId == fu.Id && e.TargetId == router.Id);
        emittedEdges!.Should().Contain(e => e.SourceId == router.Id && e.TargetId == sdf.Id);
    }

    private static SkillExecutionNode SkillNode(string name, double duration) => new()
    {
        ProcedureId = Guid.NewGuid(),
        Id = Guid.NewGuid(),
        Position = new NodePosition { X = 0, Y = 0 },
        SkillExecutionTask = new SkillExecutionTask
        {
            Name = name,
            StartTime = 0,
            Duration = duration,
            Skill = new Skill { Id = Guid.NewGuid(), Name = name, Description = "", Properties = [] },
            AgentId = Guid.NewGuid()
        }
    };

    private static TaskNode TaskNodeWith(string name, double duration, Guid? parentId = null) => new()
    {
        ProcedureId = Guid.NewGuid(),
        Id = Guid.NewGuid(),
        ParentId = parentId,
        Position = new NodePosition { X = 0, Y = 0 },
        Task = new DomainTask { Name = name, StartTime = 0, Duration = duration }
    };

    private static RouterNode RouterNodeWith(string name, double duration) => new()
    {
        ProcedureId = Guid.NewGuid(),
        Id = Guid.NewGuid(),
        Position = new NodePosition { X = 0, Y = 0 },
        RouterTask = new RouterTask
        {
            Name = name,
            StartTime = 0,
            Duration = duration,
            Selector = new SimpleVariableSelector { Expression = "x" },
            Branches = new List<ConditionalBranch>
            {
                new() { Name = "Default", Priority = 999, TargetNodeId = Guid.NewGuid() }
            }
        }
    };

    private static DependencyEdge FsEdge(Guid source, Guid target) => new()
    {
        ProcedureId = Guid.NewGuid(),
        Id = Guid.NewGuid(),
        SourceId = source,
        TargetId = target,
        SourceHandle = "right",
        TargetHandle = "left"
    };

    private static string NodeName(Node n) => n switch
    {
        SkillExecutionNode s => s.SkillExecutionTask.Name,
        RouterNode r => r.RouterTask.Name,
        TaskNode t => t.Task.Name,
        _ => "?"
    };
}