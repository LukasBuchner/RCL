using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Services.Providers;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Application.Services.Execution.Dependencies;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Routing;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ApplicationProgress = FHOOE.Freydis.Application.Services.Execution.Coordination.SkillExecutionProgress;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Triggering;

/// <summary>
///     Tests that verify router branch filtering works correctly when skills are
///     nested inside TaskNode branch containers (the real-world hierarchy).
///
///     Real hierarchy:
///     Router "df"
///     ├── TaskNode "False Branch"
///     │   └── SkillExecutionNode "v"
///     └── TaskNode "Default Branch"
///         └── SkillExecutionNode "syx"
///
///     Bug: Both "v" (False Branch) and "syx" (Default Branch) execute even when
///     router selects only one branch. This is because DependencyGraphAnalyzer
///     may not correctly add router prerequisites for nested skills.
/// </summary>
public class ExecutionTriggerServiceBranchFilteringTests
{
    // Exact GUIDs from production data
    private static readonly Guid ProcedureId = Guid.Parse("50674170-d5dd-dc41-9dc4-055cab80acd0");

    // Router "df" and its branches
    private static readonly Guid RouterDfId = Guid.Parse("4c9403be-1bb0-5e42-873b-c144a2c6a33c");
    private static readonly Guid FalseBranchTaskId = Guid.Parse("136c1bf5-c005-344c-9323-e01de23cf88f");
    private static readonly Guid DefaultBranchTaskId = Guid.Parse("8cbe9031-700c-8f44-be38-ae23245dc0c8");
    private static readonly Guid SkillVId = Guid.Parse("ca074599-e6d8-2047-ac2b-02f6d7bf08dc"); // in False Branch
    private static readonly Guid SkillSyxId = Guid.Parse("b0fe319a-0fa0-4b41-a827-cd0982c702ef"); // in Default Branch

    private readonly Subject<ExecutionEvent> _eventSubject;
    private readonly Mock<IRuntimeAgentProvider> _mockAgentProvider;
    private readonly Mock<ISkillExecutionCoordinator> _mockCoordinator;
    private readonly Mock<ISkillExecutionEventBus> _mockEventBus;
    private readonly Mock<ILogger<ExecutionTriggerService>> _mockLogger;
    private readonly Mock<IRouterEvaluationService> _mockRouterEvaluationService;
    private readonly ExecutionTriggerService _service;
    private readonly List<Guid> _triggeredSkillIds = new();

    public ExecutionTriggerServiceBranchFilteringTests()
    {
        _mockEventBus = new Mock<ISkillExecutionEventBus>();
        _mockCoordinator = new Mock<ISkillExecutionCoordinator>();
        _mockAgentProvider = new Mock<IRuntimeAgentProvider>();
        _mockRouterEvaluationService = new Mock<IRouterEvaluationService>();
        _mockLogger = new Mock<ILogger<ExecutionTriggerService>>();
        _eventSubject = new Subject<ExecutionEvent>();

        _mockEventBus.Setup(x => x.AllEvents).Returns(_eventSubject);
        _mockEventBus.Setup(x => x.PublishEvent(It.IsAny<ExecutionEvent>()))
            .Callback<ExecutionEvent>(e => _eventSubject.OnNext(e));

        // Track which skills get triggered and simulate real coordinator behavior
        _mockCoordinator
            .Setup(x => x.ExecuteSkillAsync(
                It.IsAny<Guid>(),
                It.IsAny<Skill>(),
                It.IsAny<Guid>(),
                It.IsAny<VariableContext>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, Skill, Guid, VariableContext, CancellationToken>((skillId, _, _, _, _) =>
            {
                _triggeredSkillIds.Add(skillId);
                // Simulate real coordinator: publish Start and Finish events so the router
                // can detect branch completion and publish its own Finish event
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = skillId,
                    EventType = ExecutionEventType.Start,
                    Timestamp = DateTimeOffset.UtcNow
                });
                _eventSubject.OnNext(new ExecutionEvent
                {
                    SkillId = skillId,
                    EventType = ExecutionEventType.Finish,
                    Timestamp = DateTimeOffset.UtcNow
                });
                return Observable.Empty<ApplicationProgress>();
            });

        var branchNavigator = new RouterBranchNavigator();

        var skillTriggerHandler = new SkillTriggerHandler(
            _mockEventBus.Object,
            _mockCoordinator.Object,
            _mockAgentProvider.Object,
            NullLogger<SkillTriggerHandler>.Instance,
            NullLogger<PipelineEvents>.Instance);

        var routerTriggerHandler = new RouterTriggerHandler(
            _mockEventBus.Object,
            _mockRouterEvaluationService.Object,
            branchNavigator,
            NullLogger<RouterTriggerHandler>.Instance);

        _service = new ExecutionTriggerService(
            _mockEventBus.Object,
            skillTriggerHandler,
            routerTriggerHandler,
            branchNavigator,
            _mockLogger.Object,
            NullLogger<PipelineEvents>.Instance);
    }

    /// <summary>
    ///     Reproduces the exact bug from production: router "df" selects "Default Branch",
    ///     but both skill "v" (in False Branch) and skill "syx" (in Default Branch) execute.
    ///
    ///     The bug occurs because skills are nested inside TaskNodes, not directly under the router.
    ///     DependencyGraphAnalyzer may not correctly detect the router ancestor through the TaskNode.
    /// </summary>
    [Fact]
    public async Task RouterSelectsDefaultBranch_OnlyDefaultBranchSkillExecutes()
    {
        // Arrange - Create exact hierarchy from production data
        var routerNode = CreateRouterDf();
        var falseBranchTask = CreateFalseBranchTask();
        var defaultBranchTask = CreateDefaultBranchTask();
        var skillV = CreateSkillV(); // In False Branch
        var skillSyx = CreateSkillSyx(); // In Default Branch

        var nodes = new List<Node>
        {
            routerNode,
            falseBranchTask,
            defaultBranchTask,
            skillV,
            skillSyx
        };

        // When DependencyGraphAnalyzer doesn't find router prerequisites for nested skills,
        // both skills will have empty router prerequisites. The fallback check should still
        // filter based on actual hierarchy.
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [RouterDfId] = new()
                {
                    SkillId = RouterDfId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // Skill V - branch-internal skill reacts to router Start
                [SkillVId] = new()
                {
                    SkillId = SkillVId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = RouterDfId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // Skill Syx - branch-internal skill reacts to router Start
                [SkillSyxId] = new()
                {
                    SkillId = SkillSyxId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = RouterDfId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Router selects Default Branch (not False Branch)
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(DefaultBranchTaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(200); // Allow async evaluation

        // Assert
        // ONLY skill in Default Branch should execute (skillSyx)
        Assert.Contains(SkillSyxId, _triggeredSkillIds);

        // Skill in False Branch should NOT execute (skillV)
        Assert.DoesNotContain(SkillVId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Tests the opposite case: router selects "False Branch", only skill "v" should execute.
    /// </summary>
    [Fact]
    public async Task RouterSelectsFalseBranch_OnlyFalseBranchSkillExecutes()
    {
        // Arrange
        var routerNode = CreateRouterDf();
        var falseBranchTask = CreateFalseBranchTask();
        var defaultBranchTask = CreateDefaultBranchTask();
        var skillV = CreateSkillV();
        var skillSyx = CreateSkillSyx();

        var nodes = new List<Node>
        {
            routerNode,
            falseBranchTask,
            defaultBranchTask,
            skillV,
            skillSyx
        };

        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [RouterDfId] = new()
                {
                    SkillId = RouterDfId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [SkillVId] = new()
                {
                    SkillId = SkillVId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = RouterDfId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [SkillSyxId] = new()
                {
                    SkillId = SkillSyxId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = RouterDfId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Router selects False Branch
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(FalseBranchTaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(200);

        // Assert
        // ONLY skill in False Branch should execute (skillV)
        Assert.Contains(SkillVId, _triggeredSkillIds);

        // Skill in Default Branch should NOT execute (skillSyx)
        Assert.DoesNotContain(SkillSyxId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Tests the bug case where skills have NO router prerequisites at all.
    ///     The fallback check in IsSelectedBranch should still filter correctly
    ///     by directly traversing the node hierarchy.
    /// </summary>
    [Fact]
    public async Task RouterSelectsDefaultBranch_NoRouterPrerequisites_FallbackFiltersCorrectly()
    {
        // Arrange
        var routerNode = CreateRouterDf();
        var falseBranchTask = CreateFalseBranchTask();
        var defaultBranchTask = CreateDefaultBranchTask();
        var skillV = CreateSkillV();
        var skillSyx = CreateSkillSyx();

        var nodes = new List<Node>
        {
            routerNode,
            falseBranchTask,
            defaultBranchTask,
            skillV,
            skillSyx
        };

        // BUG REPRODUCTION: Skills have NO router prerequisites
        // This is what happens when DependencyGraphAnalyzer.FindAncestorRouter fails
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [RouterDfId] = new()
                {
                    SkillId = RouterDfId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                // Skills have EMPTY prerequisites - the bug!
                [SkillVId] = new()
                {
                    SkillId = SkillVId,
                    StartPrerequisites = new List<EventPrerequisite>(), // EMPTY!
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [SkillSyxId] = new()
                {
                    SkillId = SkillSyxId,
                    StartPrerequisites = new List<EventPrerequisite>(), // EMPTY!
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();

        // Router selects Default Branch
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(It.IsAny<RouterNode>(), variableContext))
            .ReturnsAsync(DefaultBranchTaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(200);

        // Assert - The FALLBACK check should filter correctly based on hierarchy
        // Without the fix, BOTH skills would execute (the bug!)
        // With the fix, only skillSyx (in Default Branch) should execute
        Assert.Contains(SkillSyxId, _triggeredSkillIds);
        Assert.DoesNotContain(SkillVId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that a nested router living in the <b>selected</b> branch of an outer router
    ///     is evaluated correctly, even when the dependency graph contains no explicit prerequisite
    ///     linking the nested router to the outer router's Start event.
    ///
    ///     <para>
    ///         <b>Root cause without fix:</b> both routers have empty <c>StartPrerequisites</c>,
    ///         so both are classified as immediate-start nodes. During the second pass of
    ///         <see cref="ExecutionTriggerService.Start"/>, both fire synchronously. The outer
    ///         router enters <c>TriggerRouter</c> (async void, yields at <c>await EvaluateRouterAsync</c>),
    ///         then the nested router fires immediately — but the outer router hasn't stored its
    ///         branch selection yet. <c>IsSelectedBranch</c> sees "router hasn't made selection yet"
    ///         and blocks the nested router. The one-shot <c>Observable.Return</c> is consumed and
    ///         never retries, leaving the nested router permanently stuck.
    ///     </para>
    ///
    ///     Hierarchy:
    ///     <code>
    ///     OuterRouter (immediate-start, selects BranchB)
    ///     ├── BranchA_Task → SkillA (should NOT execute)
    ///     └── BranchB_Task (SELECTED)
    ///         └── NestedRouter2 (no explicit prereqs — the bug!)
    ///             ├── NB1_Task (SELECTED) → SkillB (should execute)
    ///             └── NB2_Task (empty)
    ///     </code>
    /// </summary>
    [Fact]
    public async Task NestedRouterInSelectedBranch_WithNoExplicitPrerequisite_MustStillEvaluate()
    {
        // Arrange — build the hierarchy
        var outerRouterId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid();
        var branchBTaskId = Guid.NewGuid();
        var nestedRouterId = Guid.NewGuid();
        var nb1TaskId = Guid.NewGuid();
        var nb2TaskId = Guid.NewGuid();
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = ProcedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "OuterRouter",
                Description = "Outer router selecting the branch that contains a nested router",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "mode" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "BranchA", Condition = "mode == \"simple\"", Priority = 0, TargetNodeId = branchATaskId
                    },
                    new()
                    {
                        Name = "BranchB", Condition = "mode == \"complex\"", Priority = 1, TargetNodeId = branchBTaskId
                    }
                }
            }
        };

        var branchATask = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = ProcedureId,
            ParentId = outerRouterId,
            Position = new NodePosition { X = 0, Y = 66 },
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "BranchA Task", StartTime = 0, Duration = 30 }
        };

        var skillA = new SkillExecutionNode
        {
            Id = skillAId,
            ProcedureId = ProcedureId,
            ParentId = branchATaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillA",
                StartTime = 0,
                Duration = 30,
                AgentId = agentId,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position",
                    Description = "Skill in non-selected branch",
                    Properties = new List<TypedProperty>()
                }
            }
        };

        var branchBTask = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = ProcedureId,
            ParentId = outerRouterId,
            Position = new NodePosition { X = 200, Y = 66 },
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "BranchB Task", StartTime = 0, Duration = 60 }
        };

        var nestedRouter = new RouterNode
        {
            Id = nestedRouterId,
            ProcedureId = ProcedureId,
            ParentId = branchBTaskId, // Inside BranchB (the selected branch)
            Position = new NodePosition { X = 0, Y = 40 },
            RouterTask = new RouterTask
            {
                Name = "NestedRouter2",
                Description = "Nested router — should evaluate when outer selects BranchB",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches = new List<ConditionalBranch>
                {
                    new() { Name = "NB1", Condition = "quality == \"good\"", Priority = 0, TargetNodeId = nb1TaskId },
                    new() { Name = "NB2", Condition = null, Priority = 999, TargetNodeId = nb2TaskId }
                }
            }
        };

        var nb1Task = new TaskNode
        {
            Id = nb1TaskId,
            ProcedureId = ProcedureId,
            ParentId = nestedRouterId,
            Position = new NodePosition { X = 0, Y = 40 },
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "NB1 Task", StartTime = 0, Duration = 20 }
        };

        var nb2Task = new TaskNode
        {
            Id = nb2TaskId,
            ProcedureId = ProcedureId,
            ParentId = nestedRouterId,
            Position = new NodePosition { X = 200, Y = 40 },
            Task = new Freydis.Domain.Entities.Procedure.Task { Name = "NB2 Task", StartTime = 0, Duration = 20 }
        };

        var skillB = new SkillExecutionNode
        {
            Id = skillBId,
            ProcedureId = ProcedureId,
            ParentId = nb1TaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillB",
                StartTime = 0,
                Duration = 20,
                AgentId = agentId,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Grasp Object",
                    Description = "Skill in nested selected branch",
                    Properties = new List<TypedProperty>()
                }
            }
        };

        var nodes = new List<Node>
        {
            outerRouter, branchATask, branchBTask, skillA,
            nestedRouter, nb1Task, nb2Task, skillB
        };

        // Dependency graph: NestedRouter2 has a sequencing gate prerequisite on OuterRouter.
        // The analyzer injects an SS start prerequisite, ensuring the nested router
        // waits for the outer router to evaluate before starting.
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [outerRouterId] = new()
                {
                    SkillId = outerRouterId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = outerRouterId,
                            RequiredEventType = EventTriggerType.Start,
                            DependencyType = DependencyType.StartToStart
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [nestedRouterId] = new()
                {
                    SkillId = nestedRouterId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = outerRouterId,
                            RequiredEventType = EventTriggerType.Start,
                            DependencyType = DependencyType.StartToStart
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = nestedRouterId,
                            RequiredEventType = EventTriggerType.Start,
                            DependencyType = DependencyType.StartToStart
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();
        variableContext.SetValue("mode", "complex");
        variableContext.SetValue("quality", "good");

        // OuterRouter selects BranchB (which contains NestedRouter2).
        // IMPORTANT: use Task.Delay to force a true async yield, simulating real production
        // behaviour where BranchSelector/variable evaluation is genuinely async.
        // With ReturnsAsync (completed Task), TriggerRouter never yields and runs fully
        // synchronously — hiding the race condition.
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == outerRouterId), It.IsAny<VariableContext>()))
            .Returns(async (RouterNode _, VariableContext _) =>
            {
                await Task.Yield(); // Force a true async yield
                return branchBTaskId;
            });

        // NestedRouter2 selects NB1
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == nestedRouterId), It.IsAny<VariableContext>()))
            .ReturnsAsync(nb1TaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(1000); // Extra time for async chain to settle

        // Assert — NestedRouter2 MUST be evaluated (it's in the selected branch)
        _mockRouterEvaluationService.Verify(
            x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == nestedRouterId), It.IsAny<VariableContext>()),
            Times.Once,
            "NestedRouter2 is in OuterRouter's selected branch — it must be evaluated");

        // Assert — SkillB (in NB1, selected by NestedRouter2) must execute
        Assert.Contains(skillBId, _triggeredSkillIds);

        // Assert — SkillA (in non-selected BranchA) must NOT execute
        Assert.DoesNotContain(skillAId, _triggeredSkillIds);
    }

    /// <summary>
    ///     Verifies that when an outer router selects a branch that does NOT contain a nested
    ///     router, a nested router living inside a different (non-selected) branch is never
    ///     evaluated — even if that nested router's condition would match.
    ///
    ///     Hierarchy:
    ///     <code>
    ///     OuterRouter
    ///     ├── BranchA_Task  (SELECTED — plain branch, no nested router)
    ///     │   └── SkillA
    ///     └── BranchB_Task  (NOT SELECTED — contains a nested router)
    ///         └── NestedRouter2 (condition would evaluate to true)
    ///             ├── NestedBranch1_Task → SkillB
    ///             └── NestedBranch2_Task → SkillC
    ///     </code>
    ///
    ///     Expected: OuterRouter evaluates, selects BranchA. SkillA runs.
    ///     NestedRouter2 must NOT be evaluated. SkillB and SkillC must NOT run.
    /// </summary>
    [Fact]
    public async Task OuterRouterSelectsBranchWithoutNestedRouter_NestedRouterInOtherBranchNeverEvaluated()
    {
        // Arrange — build the hierarchy
        var outerRouterId = Guid.NewGuid();
        var branchATaskId = Guid.NewGuid();
        var branchBTaskId = Guid.NewGuid();
        var nestedRouterId = Guid.NewGuid();
        var nestedBranch1TaskId = Guid.NewGuid();
        var nestedBranch2TaskId = Guid.NewGuid();
        var skillAId = Guid.NewGuid();
        var skillBId = Guid.NewGuid();
        var skillCId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        // OuterRouter — selects BranchA
        var outerRouter = new RouterNode
        {
            Id = outerRouterId,
            ProcedureId = ProcedureId,
            ParentId = null,
            Position = new NodePosition { X = 0, Y = 0 },
            RouterTask = new RouterTask
            {
                Name = "OuterRouter",
                Description = "Outer router that selects the plain branch",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "mode" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "BranchA",
                        Condition = "mode == \"simple\"",
                        Priority = 0,
                        TargetNodeId = branchATaskId
                    },
                    new()
                    {
                        Name = "BranchB",
                        Condition = "mode == \"complex\"",
                        Priority = 1,
                        TargetNodeId = branchBTaskId
                    }
                }
            }
        };

        // BranchA_Task — selected branch, plain (no nested router)
        var branchATask = new TaskNode
        {
            Id = branchATaskId,
            ProcedureId = ProcedureId,
            ParentId = outerRouterId,
            Position = new NodePosition { X = 0, Y = 66 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchA Task",
                Description = "Selected branch — no nested router",
                StartTime = 0,
                Duration = 30
            }
        };

        // SkillA — inside BranchA_Task
        var skillA = new SkillExecutionNode
        {
            Id = skillAId,
            ProcedureId = ProcedureId,
            ParentId = branchATaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillA",
                StartTime = 0,
                Duration = 30,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Move To Position",
                    Description = "Skill in selected branch",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        // BranchB_Task — non-selected branch, contains a nested router
        var branchBTask = new TaskNode
        {
            Id = branchBTaskId,
            ProcedureId = ProcedureId,
            ParentId = outerRouterId,
            Position = new NodePosition { X = 200, Y = 66 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "BranchB Task",
                Description = "Non-selected branch — contains nested router",
                StartTime = 0,
                Duration = 60
            }
        };

        // NestedRouter2 — inside BranchB_Task (non-selected branch),
        // whose condition WOULD match if evaluated
        var nestedRouter = new RouterNode
        {
            Id = nestedRouterId,
            ProcedureId = ProcedureId,
            ParentId = branchBTaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            RouterTask = new RouterTask
            {
                Name = "NestedRouter2",
                Description = "Nested router in non-selected branch — should never evaluate",
                StartTime = 0,
                Duration = 0,
                Selector = new SimpleVariableSelector { Expression = "quality" },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "NestedBranch1",
                        Condition = "quality == \"good\"",
                        Priority = 0,
                        TargetNodeId = nestedBranch1TaskId
                    },
                    new()
                    {
                        Name = "NestedBranch2",
                        Condition = null,
                        Priority = 999,
                        TargetNodeId = nestedBranch2TaskId
                    }
                }
            }
        };

        // NestedBranch1_Task — inside NestedRouter2
        var nestedBranch1Task = new TaskNode
        {
            Id = nestedBranch1TaskId,
            ProcedureId = ProcedureId,
            ParentId = nestedRouterId,
            Position = new NodePosition { X = 0, Y = 40 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "NestedBranch1 Task",
                Description = "First branch of nested router",
                StartTime = 0,
                Duration = 20
            }
        };

        // NestedBranch2_Task — inside NestedRouter2
        var nestedBranch2Task = new TaskNode
        {
            Id = nestedBranch2TaskId,
            ProcedureId = ProcedureId,
            ParentId = nestedRouterId,
            Position = new NodePosition { X = 200, Y = 40 },
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "NestedBranch2 Task",
                Description = "Second branch of nested router",
                StartTime = 0,
                Duration = 20
            }
        };

        // SkillB — inside NestedBranch1_Task
        var skillB = new SkillExecutionNode
        {
            Id = skillBId,
            ProcedureId = ProcedureId,
            ParentId = nestedBranch1TaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillB",
                StartTime = 0,
                Duration = 20,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Grasp Object",
                    Description = "Skill in nested branch 1 — should never execute",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        // SkillC — inside NestedBranch2_Task
        var skillC = new SkillExecutionNode
        {
            Id = skillCId,
            ProcedureId = ProcedureId,
            ParentId = nestedBranch2TaskId,
            Position = new NodePosition { X = 0, Y = 40 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "SkillC",
                StartTime = 0,
                Duration = 20,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = "Release Object",
                    Description = "Skill in nested branch 2 — should never execute",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };

        var nodes = new List<Node>
        {
            outerRouter, branchATask, branchBTask, skillA,
            nestedRouter, nestedBranch1Task, nestedBranch2Task, skillB, skillC
        };

        // Dependency graph: skills and nested router depend on their ancestor router's Start event.
        // OuterRouter is immediate-start (no prerequisites).
        // NestedRouter2 depends on OuterRouter.Start (DependencyGraphAnalyzer adds this for nested routers).
        // SkillA depends on OuterRouter.Start (it's inside a branch of OuterRouter).
        // SkillB/SkillC depend on NestedRouter2.Start (they're inside NestedRouter2's branches).
        var dependencyGraph = new DependencyGraph
        {
            Prerequisites = new Dictionary<Guid, SkillEventPrerequisites>
            {
                [outerRouterId] = new()
                {
                    SkillId = outerRouterId,
                    StartPrerequisites = new List<EventPrerequisite>(),
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillAId] = new()
                {
                    SkillId = skillAId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = outerRouterId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [nestedRouterId] = new()
                {
                    SkillId = nestedRouterId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = outerRouterId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillBId] = new()
                {
                    SkillId = skillBId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = nestedRouterId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                },
                [skillCId] = new()
                {
                    SkillId = skillCId,
                    StartPrerequisites = new List<EventPrerequisite>
                    {
                        new()
                        {
                            DependencySkillId = nestedRouterId,
                            RequiredEventType = EventTriggerType.Start
                        }
                    },
                    FinishPrerequisites = new List<EventPrerequisite>()
                }
            }
        };

        var variableContext = new VariableContext();
        variableContext.SetValue("mode", "simple"); // OuterRouter matches BranchA
        variableContext.SetValue("quality",
            "good"); // NestedRouter2 WOULD match NestedBranch1 — but must never be reached

        // OuterRouter evaluates → selects BranchA_Task
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == outerRouterId),
                It.IsAny<VariableContext>()))
            .ReturnsAsync(branchATaskId);

        // NestedRouter2 WOULD select NestedBranch1_Task — but this should never be called
        _mockRouterEvaluationService
            .Setup(x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == nestedRouterId),
                It.IsAny<VariableContext>()))
            .ReturnsAsync(nestedBranch1TaskId);

        // Act
        _service.Start(dependencyGraph, nodes, variableContext);
        await Task.Delay(300); // Allow full execution pipeline to settle

        // Assert — SkillA (selected branch) must execute
        Assert.Contains(skillAId, _triggeredSkillIds);

        // Assert — NestedRouter2 must NEVER be evaluated (it's in a non-selected branch)
        _mockRouterEvaluationService.Verify(
            x => x.EvaluateRouterAsync(
                It.Is<RouterNode>(r => r.Id == nestedRouterId),
                It.IsAny<VariableContext>()),
            Times.Never,
            "NestedRouter2 is inside a non-selected branch of OuterRouter — it must never be evaluated");

        // Assert — SkillB and SkillC (inside NestedRouter2's branches) must NOT execute
        Assert.DoesNotContain(skillBId, _triggeredSkillIds);
        Assert.DoesNotContain(skillCId, _triggeredSkillIds);
    }

    #region Helper Methods - Create exact production node structure

    private RouterNode CreateRouterDf()
    {
        return new RouterNode
        {
            Id = RouterDfId,
            ProcedureId = ProcedureId,
            ParentId = null,
            Position = new NodePosition { X = 80, Y = 492 },
            Width = 110,
            Height = 286,
            Hidden = false,
            RouterTask = new RouterTask
            {
                Name = "df",
                Description = "Router: df",
                StartTime = 40,
                Duration = 55,
                Selector = new SimpleVariableSelector
                {
                    Expression = ""
                },
                Branches = new List<ConditionalBranch>
                {
                    new()
                    {
                        Name = "False",
                        Condition = "QualityOK == \"false\"",
                        Priority = 0,
                        TargetNodeId = FalseBranchTaskId
                    },
                    new()
                    {
                        Name = "Default",
                        Condition = null,
                        Priority = 999,
                        TargetNodeId = DefaultBranchTaskId
                    }
                },
                SelectedBranchTargetNodeId = DefaultBranchTaskId,
                SelectedBranchName = "Default",
                SelectedAtUtc = DateTime.UtcNow
            }
        };
    }

    private TaskNode CreateFalseBranchTask()
    {
        return new TaskNode
        {
            Id = FalseBranchTaskId,
            ProcedureId = ProcedureId,
            ParentId = RouterDfId, // Parent is the router!
            Position = new NodePosition { X = 0, Y = 66 },
            Width = 110,
            Height = 100,
            Hidden = false,
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "False Branch",
                Description = "Branch target for 'False' from router 'df'",
                StartTime = 40,
                Duration = 55
            }
        };
    }

    private TaskNode CreateDefaultBranchTask()
    {
        return new TaskNode
        {
            Id = DefaultBranchTaskId,
            ProcedureId = ProcedureId,
            ParentId = RouterDfId, // Parent is the router!
            Position = new NodePosition { X = 0, Y = 66 },
            Width = 110,
            Height = 100,
            Hidden = true,
            Task = new Freydis.Domain.Entities.Procedure.Task
            {
                Name = "Default Branch",
                Description = "Branch target for 'Default' from router 'df'",
                StartTime = 0,
                Duration = 55
            }
        };
    }

    private SkillExecutionNode CreateSkillV()
    {
        var agentId = new Guid(Convert.FromBase64String("3vEjRWeJTe+Jq0VniQq83g=="));
        var skillId = Guid.NewGuid();

        return new SkillExecutionNode
        {
            Id = SkillVId,
            ProcedureId = ProcedureId,
            ParentId = FalseBranchTaskId, // Parent is False Branch TaskNode!
            Position = new NodePosition { X = 0, Y = 40 },
            Width = 110,
            Height = 50,
            Hidden = false,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "v",
                StartTime = 40,
                Duration = 55,
                Skill = new Skill
                {
                    Id = skillId,
                    Name = "Grasp Object",
                    Description = "Grasp an object",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };
    }

    private SkillExecutionNode CreateSkillSyx()
    {
        var agentId = new Guid(Convert.FromBase64String("3vEjRWeJTe+Jq0VniQq83g=="));
        var skillId = Guid.NewGuid();

        return new SkillExecutionNode
        {
            Id = SkillSyxId,
            ProcedureId = ProcedureId,
            ParentId = DefaultBranchTaskId, // Parent is Default Branch TaskNode!
            Position = new NodePosition { X = 0, Y = 40 },
            Width = 110,
            Height = 50,
            Hidden = true,
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = "syx",
                StartTime = 0,
                Duration = 55,
                Skill = new Skill
                {
                    Id = skillId,
                    Name = "Grasp Object",
                    Description = "Grasp an object",
                    Properties = new List<TypedProperty>()
                },
                AgentId = agentId
            }
        };
    }

    #endregion
}