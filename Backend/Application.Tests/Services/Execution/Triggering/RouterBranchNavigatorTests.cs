using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Triggering;

/// <summary>
///     Tests for <see cref="RouterBranchNavigator" />, verifying hierarchy traversal
///     for branch navigation, ancestor router lookup, and branch membership.
/// </summary>
public class RouterBranchNavigatorTests
{
    private readonly RouterBranchNavigator _sut = new();

    // ─── FindDirectExecutableNodesInBranch ───────────────────────────

    [Fact]
    public void FindDirectExecutableNodesInBranch_SkillTarget_ReturnsThatSkill()
    {
        var skillId = Guid.NewGuid();
        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Skill(skillId, "Skill A"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindDirectExecutableNodesInBranch(skillId);

        Assert.Single(result);
        Assert.Equal(skillId, result[0]);
    }

    [Fact]
    public void FindDirectExecutableNodesInBranch_TaskWithChildSkills_ReturnsChildSkills()
    {
        var parentId = Guid.NewGuid();
        var skill1Id = Guid.NewGuid();
        var skill2Id = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(parentId, "Parent"),
            Skill(skill1Id, "Skill 1", parentId),
            Skill(skill2Id, "Skill 2", parentId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindDirectExecutableNodesInBranch(parentId);

        Assert.Equal(2, result.Count);
        Assert.Contains(skill1Id, result);
        Assert.Contains(skill2Id, result);
    }

    [Fact]
    public void FindDirectExecutableNodesInBranch_NestedRouter_IncludesRouterButNotItsChildren()
    {
        var parentId = Guid.NewGuid();
        var nestedRouterId = Guid.NewGuid();
        var deepSkillId = Guid.NewGuid();
        var siblingSkillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(parentId, "Branch Root"),
            Router(nestedRouterId, "Nested Router", parentId),
            Skill(deepSkillId, "Deep Skill", nestedRouterId),
            Skill(siblingSkillId, "Sibling Skill", parentId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindDirectExecutableNodesInBranch(parentId);

        Assert.Equal(2, result.Count);
        Assert.Contains(nestedRouterId, result); // Router included
        Assert.Contains(siblingSkillId, result); // Sibling included
        Assert.DoesNotContain(deepSkillId, result); // Router's child excluded
    }

    [Fact]
    public void FindDirectExecutableNodesInBranch_EmptyBranch_ReturnsEmptyList()
    {
        var parentId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(parentId, "Empty Parent"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindDirectExecutableNodesInBranch(parentId);

        Assert.Empty(result);
    }

    [Fact]
    public void FindDirectExecutableNodesInBranch_BranchTargetIsRouter_TraversesItsChildren()
    {
        // When the selected branch target IS a router (external sequential),
        // it should traverse the router's children, not stop at it.
        var routerId = Guid.NewGuid();
        var childSkillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Router(routerId, "External Router"),
            Skill(childSkillId, "Child Skill", routerId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindDirectExecutableNodesInBranch(routerId);

        Assert.Single(result);
        Assert.Equal(childSkillId, result[0]);
    }

    // ─── FindAllDescendantExecutableNodes ────────────────────────────

    [Fact]
    public void FindAllDescendantExecutableNodes_IncludesNestedRouterChildren()
    {
        var parentId = Guid.NewGuid();
        var nestedRouterId = Guid.NewGuid();
        var deepSkillId = Guid.NewGuid();
        var siblingSkillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(parentId, "Branch Root"),
            Router(nestedRouterId, "Nested Router", parentId),
            Skill(deepSkillId, "Deep Skill", nestedRouterId),
            Skill(siblingSkillId, "Sibling Skill", parentId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAllDescendantExecutableNodes(parentId);

        Assert.Equal(3, result.Count); // nested router + deep skill + sibling
        Assert.Contains(nestedRouterId, result);
        Assert.Contains(deepSkillId, result);
        Assert.Contains(siblingSkillId, result);
    }

    [Fact]
    public void FindAllDescendantExecutableNodes_DeeplyNestedHierarchy_FindsAll()
    {
        var root = Guid.NewGuid();
        var mid = Guid.NewGuid();
        var deepSkill = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(root, "Root"),
            TaskN(mid, "Mid", root),
            Skill(deepSkill, "Deep", mid));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAllDescendantExecutableNodes(root);

        Assert.Single(result);
        Assert.Equal(deepSkill, result[0]);
    }

    // ─── FindAncestorRouter ──────────────────────────────────────────

    [Fact]
    public void FindAncestorRouter_NodeInsideRouter_ReturnsRouter()
    {
        var routerId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Router(routerId, "Parent Router"),
            Skill(skillId, "Child Skill", routerId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAncestorRouter(skillId);

        Assert.NotNull(result);
        Assert.Equal(routerId, result!.Id);
    }

    [Fact]
    public void FindAncestorRouter_NodeNotInsideRouter_ReturnsNull()
    {
        var skillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Skill(skillId, "Standalone Skill"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAncestorRouter(skillId);

        Assert.Null(result);
    }

    [Fact]
    public void FindAncestorRouter_DeeplyNestedNode_FindsClosestRouter()
    {
        var routerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Router(routerId, "Top Router"),
            TaskN(taskId, "Mid Task", routerId),
            Skill(skillId, "Deep Skill", taskId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAncestorRouter(skillId);

        Assert.NotNull(result);
        Assert.Equal(routerId, result!.Id);
    }

    [Fact]
    public void FindAncestorRouter_UnknownNodeId_ReturnsNull()
    {
        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Skill(Guid.NewGuid(), "Some Skill"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        var result = _sut.FindAncestorRouter(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── IsNodeInSelectedBranch ──────────────────────────────────────

    [Fact]
    public void IsNodeInSelectedBranch_DirectMatch_ReturnsTrue()
    {
        var targetId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            Skill(targetId, "Target Skill"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        Assert.True(_sut.IsNodeInSelectedBranch(targetId, targetId));
    }

    [Fact]
    public void IsNodeInSelectedBranch_ChildOfTarget_ReturnsTrue()
    {
        var targetId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(targetId, "Target"),
            Skill(childId, "Child", targetId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        Assert.True(_sut.IsNodeInSelectedBranch(childId, targetId));
    }

    [Fact]
    public void IsNodeInSelectedBranch_GrandchildOfTarget_ReturnsTrue()
    {
        var targetId = Guid.NewGuid();
        var midId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(targetId, "Target"),
            TaskN(midId, "Mid", targetId),
            Skill(grandchildId, "Grandchild", midId));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        Assert.True(_sut.IsNodeInSelectedBranch(grandchildId, targetId));
    }

    [Fact]
    public void IsNodeInSelectedBranch_NodeInDifferentBranch_ReturnsFalse()
    {
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var skill1 = Guid.NewGuid();
        var skill2 = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(branch1, "Branch 1"),
            TaskN(branch2, "Branch 2"),
            Skill(skill1, "Skill in B1", branch1),
            Skill(skill2, "Skill in B2", branch2));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        Assert.False(_sut.IsNodeInSelectedBranch(skill2, branch1));
    }

    [Fact]
    public void IsNodeInSelectedBranch_UnknownNode_ReturnsFalse()
    {
        var targetId = Guid.NewGuid();

        var (allNodes, skillNodes, routerNodes) = BuildHierarchy(
            TaskN(targetId, "Target"));

        _sut.Initialize(allNodes, skillNodes, routerNodes);

        Assert.False(_sut.IsNodeInSelectedBranch(Guid.NewGuid(), targetId));
    }

    // ─── Initialize validation ───────────────────────────────────────

    [Fact]
    public void Initialize_NullAllNodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.Initialize(null!, new Dictionary<Guid, SkillExecutionNode>(),
                new Dictionary<Guid, RouterNode>()));
    }

    [Fact]
    public void Initialize_NullSkillNodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.Initialize(new Dictionary<Guid, Node>(), null!,
                new Dictionary<Guid, RouterNode>()));
    }

    [Fact]
    public void Initialize_NullRouterNodes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.Initialize(new Dictionary<Guid, Node>(),
                new Dictionary<Guid, SkillExecutionNode>(), null!));
    }

    private static NodeSpec Skill(Guid id, string name, Guid? parentId = null)
    {
        return new NodeSpec(new SkillExecutionNode
        {
            Id = id,
            ProcedureId = Guid.Empty,
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                Duration = 5.0,
                StartTime = 0,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = name,
                    Properties = []
                }
            }
        });
    }

    private static NodeSpec TaskN(Guid id, string name, Guid? parentId = null)
    {
        return new NodeSpec(new TaskNode
        {
            Id = id,
            ProcedureId = Guid.Empty,
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            Task = new Task
            {
                Name = name,
                Duration = 0,
                StartTime = 0
            }
        });
    }

    private static NodeSpec Router(Guid id, string name, Guid? parentId = null)
    {
        return new NodeSpec(new RouterNode
        {
            Id = id,
            ProcedureId = Guid.Empty,
            Position = new NodePosition { X = 0, Y = 0 },
            ParentId = parentId,
            RouterTask = new RouterTask
            {
                Name = name,
                Duration = 0,
                StartTime = 0,
                Selector = new SimpleVariableSelector { Expression = "x" },
                Branches = []
            }
        });
    }

    private static (
        IReadOnlyDictionary<Guid, Node> allNodes,
        IReadOnlyDictionary<Guid, SkillExecutionNode> skillNodes,
        IReadOnlyDictionary<Guid, RouterNode> routerNodes)
        BuildHierarchy(params NodeSpec[] specs)
    {
        var allNodes = specs.ToDictionary(s => s.Node.Id, s => s.Node);
        var skillNodes = specs
            .Where(s => s.Node is SkillExecutionNode)
            .ToDictionary(s => s.Node.Id, s => (SkillExecutionNode)s.Node);
        var routerNodes = specs
            .Where(s => s.Node is RouterNode)
            .ToDictionary(s => s.Node.Id, s => (RouterNode)s.Node);

        return (allNodes, skillNodes, routerNodes);
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private sealed record NodeSpec(Node Node);
}