using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Scheduling.Processing.Hierarchy;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common;

/// <summary>
///     Verifies that <see cref="NodeResolver" /> resolves a node to its executable descendants per the canonical
///     rule: a skill or router resolves to itself, a task resolves to the union of its descendants' executable IDs
///     (recursing through nested tasks), and a router terminates the branch (its subtree is not expanded).
/// </summary>
public sealed class NodeResolverTests
{
    private readonly NodeResolver _resolver = new(NullLogger<NodeResolver>.Instance);

    [Fact]
    public void ResolveToExecutableIds_SkillNode_ReturnsItself()
    {
        var skill = CreateSkill();
        var hierarchy = BuildHierarchy(skill);

        _resolver.ResolveToExecutableIds(skill.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(skill.Id);
    }

    [Fact]
    public void ResolveToExecutableIds_RouterNode_ReturnsItself()
    {
        var router = CreateRouter();
        var hierarchy = BuildHierarchy(router);

        _resolver.ResolveToExecutableIds(router.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(router.Id);
    }

    [Fact]
    public void ResolveToExecutableIds_TaskWithDirectSkills_ReturnsThoseSkills()
    {
        var task = CreateTask();
        var s1 = CreateSkill(parentId: task.Id);
        var s2 = CreateSkill(parentId: task.Id);
        var hierarchy = BuildHierarchy(task, s1, s2);

        _resolver.ResolveToExecutableIds(task.Id, hierarchy)
            .Should().BeEquivalentTo([s1.Id, s2.Id]);
    }

    [Fact]
    public void ResolveToExecutableIds_TaskContainingSubTask_ReturnsNestedSkill()
    {
        // Task A -> sub-Task B -> Skill S. The single-level resolver returned empty here (the bug);
        // the recursive resolver must reach S.
        var taskA = CreateTask();
        var taskB = CreateTask(parentId: taskA.Id);
        var skillS = CreateSkill(parentId: taskB.Id);
        var hierarchy = BuildHierarchy(taskA, taskB, skillS);

        _resolver.ResolveToExecutableIds(taskA.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(skillS.Id);
    }

    [Fact]
    public void ResolveToExecutableIds_DeeplyNestedTasks_ReturnsLeafSkill()
    {
        var taskA = CreateTask();
        var taskB = CreateTask(parentId: taskA.Id);
        var taskC = CreateTask(parentId: taskB.Id);
        var skillS = CreateSkill(parentId: taskC.Id);
        var hierarchy = BuildHierarchy(taskA, taskB, taskC, skillS);

        _resolver.ResolveToExecutableIds(taskA.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(skillS.Id);
    }

    [Fact]
    public void ResolveToExecutableIds_TaskContainingRouter_ReturnsRouterIdNotBranchSkills()
    {
        // Routers publish their own Start/Finish, so resolution stops at the router and does not
        // descend into its branch subtree.
        var task = CreateTask();
        var router = CreateRouter(parentId: task.Id);
        var branchSkill = CreateSkill(parentId: router.Id);
        var hierarchy = BuildHierarchy(task, router, branchSkill);

        _resolver.ResolveToExecutableIds(task.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(router.Id);
    }

    [Fact]
    public void ResolveToExecutableIds_TaskWithMixedChildren_ReturnsSkillsAndRouters()
    {
        var task = CreateTask();
        var directSkill = CreateSkill(parentId: task.Id);
        var router = CreateRouter(parentId: task.Id);
        var subTask = CreateTask(parentId: task.Id);
        var nestedSkill = CreateSkill(parentId: subTask.Id);
        var hierarchy = BuildHierarchy(task, directSkill, router, subTask, nestedSkill);

        _resolver.ResolveToExecutableIds(task.Id, hierarchy)
            .Should().BeEquivalentTo([directSkill.Id, router.Id, nestedSkill.Id]);
    }

    [Fact]
    public void ResolveToExecutableIds_EmptyTask_ReturnsEmpty()
    {
        var task = CreateTask();
        var hierarchy = BuildHierarchy(task);

        _resolver.ResolveToExecutableIds(task.Id, hierarchy).Should().BeEmpty();
    }

    [Fact]
    public void ResolveToExecutableIds_UnknownId_ReturnsEmpty()
    {
        var hierarchy = BuildHierarchy(CreateSkill());

        _resolver.ResolveToExecutableIds(Guid.NewGuid(), hierarchy).Should().BeEmpty();
    }

    [Fact]
    public void ResolveToFiringEndpoints_SkillNode_ReturnsItself()
    {
        var skill = CreateSkill();
        var hierarchy = BuildHierarchy(skill);

        _resolver.ResolveToFiringEndpointsIds(skill.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(skill.Id);
    }

    [Fact]
    public void ResolveToFiringEndpoints_TaskWithSkills_ReturnsSkills()
    {
        var task = CreateTask();
        var s1 = CreateSkill(parentId: task.Id);
        var s2 = CreateSkill(parentId: task.Id);
        var hierarchy = BuildHierarchy(task, s1, s2);

        _resolver.ResolveToFiringEndpointsIds(task.Id, hierarchy)
            .Should().BeEquivalentTo([s1.Id, s2.Id]);
    }

    [Fact]
    public void ResolveToFiringEndpoints_LeaflessTask_ReturnsItself()
    {
        // A leafless container is its own zero-extent firing endpoint, so a dependency through it gates on
        // the container instead of being dropped. Complements ResolveToExecutableIds_EmptyTask_ReturnsEmpty.
        var task = CreateTask();
        var hierarchy = BuildHierarchy(task);

        _resolver.ResolveToFiringEndpointsIds(task.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(task.Id);
    }

    [Fact]
    public void ResolveToFiringEndpoints_NestedLeaflessTasks_EachResolvesToItself()
    {
        // An outer container holds an empty inner task (both leafless). The flat wrapper resolves each
        // leafless node to itself: the outer resolves to the outer (not the inner), and the inner resolves
        // to itself, so an edge to the outer gates on the outer with no ancestor walk.
        var outer = CreateTask();
        var inner = CreateTask(parentId: outer.Id);
        var hierarchy = BuildHierarchy(outer, inner);

        _resolver.ResolveToFiringEndpointsIds(outer.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(outer.Id);
        _resolver.ResolveToFiringEndpointsIds(inner.Id, hierarchy)
            .Should().ContainSingle().Which.Should().Be(inner.Id);
    }

    [Fact]
    public void ResolveToFiringEndpoints_UnknownId_ReturnsEmpty()
    {
        var hierarchy = BuildHierarchy(CreateSkill());

        _resolver.ResolveToFiringEndpointsIds(Guid.NewGuid(), hierarchy).Should().BeEmpty();
    }

    private static NodeHierarchyInfo BuildHierarchy(params Node[] nodes)
    {
        var skills = nodes.OfType<SkillExecutionNode>().ToList().AsReadOnly();
        return new NodeHierarchyInfo
        {
            TaskNodes = nodes.OfType<TaskNode>().ToList().AsReadOnly(),
            SkillExecutionNodes = skills,
            RouterNodes = nodes.OfType<RouterNode>().ToList().AsReadOnly(),
            ParentToChildrenMapping = nodes
                .ToLookup(n => n.ParentId ?? Guid.Empty)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Node>)g.ToList().AsReadOnly()),
            TaskToSkillMapping = skills
                .Where(s => s.ParentId.HasValue)
                .GroupBy(s => s.ParentId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<SkillExecutionNode>)g.ToList().AsReadOnly()),
            SkillToTaskMapping = new Dictionary<Guid, TaskNode>()
        };
    }

    private static SkillExecutionNode CreateSkill(Guid? id = null, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "Test Skill",
                StartTime = 0,
                Duration = 1,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Test",
                    Description = "Test skill",
                    Properties = new List<TypedProperty>()
                },
                AgentId = Guid.NewGuid()
            }
        };
    }

    private static TaskNode CreateTask(Guid? id = null, Guid? parentId = null)
    {
        return new TaskNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            Task = new Task { Name = "Test Task", StartTime = 0, Duration = 1 }
        };
    }

    private static RouterNode CreateRouter(Guid? id = null, Guid? parentId = null)
    {
        return new RouterNode
        {
            ProcedureId = Guid.NewGuid(),
            Id = id ?? Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "Router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "var" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "Branch", TargetNodeId = Guid.NewGuid(), Priority = 0 }
                }
            }
        };
    }
}