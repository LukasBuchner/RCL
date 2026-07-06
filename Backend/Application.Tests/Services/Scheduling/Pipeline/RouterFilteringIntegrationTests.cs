using FHOOE.Freydis.Application.Services.Scheduling.Pipeline;
using FHOOE.Freydis.Application.Tests.TestUtilities;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling.Pipeline;

/// <summary>
///     Integration tests for router-based node filtering in the scheduling pipeline.
///     Verifies that only nodes on the selected branch path are included in scheduling results.
/// </summary>
public class RouterFilteringIntegrationTests : IDisposable
{
    private readonly ITimingCalculationOrchestrator _orchestrator;
    private readonly ServiceProvider _serviceProvider;

    public RouterFilteringIntegrationTests()
    {
        var services = new ServiceCollection();
        TestServiceConfiguration.ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = _serviceProvider.GetRequiredService<ITimingCalculationOrchestrator>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task CalculateAsync_WithRouterSelections_FiltersNonSelectedBranches()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            // Start node
            new SkillExecutionNode
            {
                Id = startNodeId,
                Position = new NodePosition
                {
                    X = 0,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Start",
                    StartTime = 0,
                    Duration = 10,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Start",
                        Description = "Start skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            // Router node
            new RouterNode
            {
                Id = routerId,
                Position = new NodePosition
                {
                    X = 100,
                    Y = 0
                },
                RouterTask = new RouterTask
                {
                    Name = "Router",
                    StartTime = 10,
                    Duration = 0,
                    Selector = new SimpleVariableSelector
                    {
                        Expression = "choice"
                    },
                    Branches = new List<ConditionalBranch>
                    {
                        new()
                        {
                            Name = "A",
                            Condition = "choice == 'A'",
                            Priority = 1,
                            TargetNodeId = branchAId
                        },
                        new()
                        {
                            Name = "B",
                            Condition = "choice == 'B'",
                            Priority = 2,
                            TargetNodeId = branchBId
                        }
                    }
                },
                ProcedureId = default
            },
            // Branch A (selected)
            new SkillExecutionNode
            {
                Id = branchAId,
                Position = new NodePosition
                {
                    X = 200,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch A",
                    StartTime = 10,
                    Duration = 15,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch A",
                        Description = "Branch A skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            // Branch B (not selected)
            new SkillExecutionNode
            {
                Id = branchBId,
                Position = new NodePosition
                {
                    X = 200,
                    Y = 100
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch B",
                    StartTime = 10,
                    Duration = 20,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch B",
                        Description = "Branch B skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            // End node
            new SkillExecutionNode
            {
                Id = endNodeId,
                Position = new NodePosition
                {
                    X = 300,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "End",
                    StartTime = 25,
                    Duration = 5,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "End",
                        Description = "End skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            }
        };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = startNodeId,
                TargetId = routerId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = routerId,
                TargetId = branchAId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = routerId,
                TargetId = branchBId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = branchAId,
                TargetId = endNodeId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = branchBId,
                TargetId = endNodeId,
                ProcedureId = default
            }
        };

        // Router selected Branch A
        var routerSelections = new Dictionary<Guid, Guid>
        {
            { routerId, branchAId }
        };

        var request = new SchedulingRequest
        {
            ProcedureId = procedureId,
            Nodes = nodes,
            Edges = edges,
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = routerSelections
        };

        // Act
        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();

        // Branch B should be in the result but marked as hidden (filtered out by router)
        var branchBNode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchBId);
        branchBNode.Should().NotBeNull("Branch B should be in the result set with hidden state");
        branchBNode!.Hidden.Should().BeTrue("Branch B was not selected by the router");

        // Branch A should be in the updated nodes and visible
        var branchANode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchAId);
        branchANode.Should().NotBeNull("Branch A was selected by the router");
        branchANode!.Hidden.Should().BeFalse("Branch A was selected by the router");

        // Start, Router, and End should all be included and visible
        result.UpdatedNodes!.Should().Contain(n => n.Id == startNodeId);
        result.UpdatedNodes!.Should().Contain(n => n.Id == routerId);
        result.UpdatedNodes!.Should().Contain(n => n.Id == endNodeId);
    }

    [Fact]
    public async Task CalculateAsync_WithNoRouterSelections_IncludesAllNodes()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var nodeAId = Guid.NewGuid();
        var nodeBId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            new SkillExecutionNode
            {
                Id = nodeAId,
                Position = new NodePosition
                {
                    X = 0,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "A",
                    StartTime = 0,
                    Duration = 10,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "A",
                        Description = "Skill A",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = nodeBId,
                Position = new NodePosition
                {
                    X = 100,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "B",
                    StartTime = 10,
                    Duration = 10,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "B",
                        Description = "Skill B",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            }
        };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = nodeAId,
                TargetId = nodeBId,
                ProcedureId = default
            }
        };

        var request = new SchedulingRequest
        {
            ProcedureId = procedureId,
            Nodes = nodes,
            Edges = edges,
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = null // No router selections
        };

        // Act
        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();
        result.UpdatedNodes!.Count.Should().Be(2, "All nodes should be included when no router selections");
    }

    [Fact]
    public async Task CalculateAsync_WithMultipleRouters_FiltersCorrectly()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var startId = Guid.NewGuid();
        var router1Id = Guid.NewGuid();
        var router1BranchAId = Guid.NewGuid();
        var router1BranchBId = Guid.NewGuid();
        var router2Id = Guid.NewGuid();
        var router2BranchXId = Guid.NewGuid();
        var router2BranchYId = Guid.NewGuid();
        var endId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            new SkillExecutionNode
            {
                Id = startId,
                Position = new NodePosition
                {
                    X = 0,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Start",
                    StartTime = 0,
                    Duration = 5,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Start",
                        Description = "Start skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new RouterNode
            {
                Id = router1Id,
                Position = new NodePosition
                {
                    X = 50,
                    Y = 0
                },
                RouterTask = new RouterTask
                {
                    Name = "Router1",
                    StartTime = 5,
                    Duration = 0,
                    Selector = new SimpleVariableSelector
                    {
                        Expression = "choice1"
                    },
                    Branches = new List<ConditionalBranch>
                    {
                        new()
                        {
                            Name = "A",
                            Condition = "choice1 == 'A'",
                            Priority = 1,
                            TargetNodeId = router1BranchAId
                        },
                        new()
                        {
                            Name = "B",
                            Condition = "choice1 == 'B'",
                            Priority = 2,
                            TargetNodeId = router1BranchBId
                        }
                    }
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = router1BranchAId,
                Position = new NodePosition
                {
                    X = 100,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch A",
                    StartTime = 5,
                    Duration = 10,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch A",
                        Description = "Branch A skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = router1BranchBId,
                Position = new NodePosition
                {
                    X = 100,
                    Y = 100
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch B",
                    StartTime = 5,
                    Duration = 15,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch B",
                        Description = "Branch B skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new RouterNode
            {
                Id = router2Id,
                Position = new NodePosition
                {
                    X = 150,
                    Y = 0
                },
                RouterTask = new RouterTask
                {
                    Name = "Router2",
                    StartTime = 15,
                    Duration = 0,
                    Selector = new SimpleVariableSelector
                    {
                        Expression = "choice2"
                    },
                    Branches = new List<ConditionalBranch>
                    {
                        new()
                        {
                            Name = "X",
                            Condition = "choice2 == 'X'",
                            Priority = 1,
                            TargetNodeId = router2BranchXId
                        },
                        new()
                        {
                            Name = "Y",
                            Condition = "choice2 == 'Y'",
                            Priority = 2,
                            TargetNodeId = router2BranchYId
                        }
                    }
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = router2BranchXId,
                Position = new NodePosition
                {
                    X = 200,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch X",
                    StartTime = 15,
                    Duration = 8,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch X",
                        Description = "Branch X skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = router2BranchYId,
                Position = new NodePosition
                {
                    X = 200,
                    Y = 100
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "Branch Y",
                    StartTime = 15,
                    Duration = 12,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "Branch Y",
                        Description = "Branch Y skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            },
            new SkillExecutionNode
            {
                Id = endId,
                Position = new NodePosition
                {
                    X = 250,
                    Y = 0
                },
                SkillExecutionTask = new SkillExecutionTask
                {
                    Name = "End",
                    StartTime = 23,
                    Duration = 5,
                    Skill = new Skill
                    {
                        Id = Guid.NewGuid(),
                        Name = "End",
                        Description = "End skill",
                        Properties = new List<TypedProperty>()
                    },
                    AgentId = Guid.NewGuid()
                },
                ProcedureId = default
            }
        };

        var edges = new List<DependencyEdge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = startId,
                TargetId = router1Id,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router1Id,
                TargetId = router1BranchAId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router1Id,
                TargetId = router1BranchBId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router1BranchAId,
                TargetId = router2Id,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router1BranchBId,
                TargetId = router2Id,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router2Id,
                TargetId = router2BranchXId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router2Id,
                TargetId = router2BranchYId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router2BranchXId,
                TargetId = endId,
                ProcedureId = default
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceId = router2BranchYId,
                TargetId = endId,
                ProcedureId = default
            }
        };

        // Router1 selected A, Router2 selected X
        var routerSelections = new Dictionary<Guid, Guid>
        {
            { router1Id, router1BranchAId },
            { router2Id, router2BranchXId }
        };

        var request = new SchedulingRequest
        {
            ProcedureId = procedureId,
            Nodes = nodes,
            Edges = edges,
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = routerSelections
        };

        // Act
        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();

        // Non-selected branches should be present but hidden
        var r1BranchB = result.UpdatedNodes!.FirstOrDefault(n => n.Id == router1BranchBId);
        r1BranchB.Should().NotBeNull("Router1 Branch B should be in result with hidden state");
        r1BranchB!.Hidden.Should().BeTrue("Router1 Branch B not selected");

        var r2BranchY = result.UpdatedNodes!.FirstOrDefault(n => n.Id == router2BranchYId);
        r2BranchY.Should().NotBeNull("Router2 Branch Y should be in result with hidden state");
        r2BranchY!.Hidden.Should().BeTrue("Router2 Branch Y not selected");

        // Selected branches should be included and visible
        var r1BranchA = result.UpdatedNodes!.FirstOrDefault(n => n.Id == router1BranchAId);
        r1BranchA.Should().NotBeNull("Router1 Branch A selected");
        r1BranchA!.Hidden.Should().BeFalse("Router1 Branch A selected");

        var r2BranchX = result.UpdatedNodes!.FirstOrDefault(n => n.Id == router2BranchXId);
        r2BranchX.Should().NotBeNull("Router2 Branch X selected");
        r2BranchX!.Hidden.Should().BeFalse("Router2 Branch X selected");

        // Other nodes should be included
        result.UpdatedNodes!.Should().Contain(n => n.Id == startId);
        result.UpdatedNodes!.Should().Contain(n => n.Id == router1Id);
        result.UpdatedNodes!.Should().Contain(n => n.Id == router2Id);
        result.UpdatedNodes!.Should().Contain(n => n.Id == endId);
    }

    /// <summary>
    ///     Verifies that design-time scheduling with ManuallySelectedBranch set
    ///     filters to only the selected branch and hides non-selected branches.
    ///     Previously this was broken because design-time never called FilterNodesAsync.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_DesignTime_WithManuallySelectedBranch_HidesNonSelectedBranches()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();

        var nodes = new List<Node>
        {
            new RouterNode
            {
                Id = routerId,
                Position = new NodePosition { X = 0, Y = 0 },
                RouterTask = new RouterTask
                {
                    Name = "QualityRouter",
                    StartTime = 0,
                    Duration = 0,
                    Selector = new SimpleVariableSelector { Expression = "QualityOK" },
                    ManuallySelectedBranch = "OK",
                    Branches = new List<ConditionalBranch>
                    {
                        new()
                        {
                            Name = "OK",
                            Condition = "QualityOK == true",
                            Priority = 1,
                            TargetNodeId = branchAId
                        },
                        new()
                        {
                            Name = "Fail",
                            Condition = "QualityOK == false",
                            Priority = 2,
                            TargetNodeId = branchBId
                        }
                    }
                },
                ProcedureId = default
            },
            // Branch "OK" child — this is the selected branch
            new TaskNode
            {
                Id = branchAId,
                ParentId = routerId,
                Extent = "parent",
                Position = new NodePosition { X = 0, Y = 30 },
                Task = new Freydis.Domain.Entities.Procedure.Task
                {
                    Name = "OK Branch",
                    StartTime = 0,
                    Duration = 200
                },
                ProcedureId = default
            },
            // Branch "Fail" child — this is NOT selected
            new TaskNode
            {
                Id = branchBId,
                ParentId = routerId,
                Extent = "parent",
                Position = new NodePosition { X = 0, Y = 100 },
                Task = new Freydis.Domain.Entities.Procedure.Task
                {
                    Name = "Fail Branch",
                    StartTime = 0,
                    Duration = 200
                },
                ProcedureId = default
            }
        };

        var request = new SchedulingRequest
        {
            ProcedureId = procedureId,
            Nodes = nodes,
            Edges = new List<DependencyEdge>(),
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = null // Design-time: no execution selections
        };

        // Act
        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();

        // The selected branch ("OK") should be visible
        var branchANode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchAId);
        branchANode.Should().NotBeNull("Selected branch 'OK' should be in the result");
        branchANode!.Hidden.Should().BeFalse("Selected branch 'OK' should be visible");

        // The non-selected branch ("Fail") should be hidden
        var branchBNode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchBId);
        branchBNode.Should().NotBeNull("Non-selected branch 'Fail' should still be in the result");
        branchBNode!.Hidden.Should().BeTrue("Non-selected branch 'Fail' should be hidden in design-time");

        // The router itself should be visible
        var routerNode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == routerId);
        routerNode.Should().NotBeNull("Router should be in the result");
        routerNode!.Hidden.Should().BeFalse("Router should be visible");
    }

    /// <summary>
    ///     Verifies that clearing ManuallySelectedBranch (setting to null) causes all branches
    ///     to become visible and clears any stale Hidden=true state from a previous selection.
    ///     This was broken because design-time never applied hidden state management.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_DesignTime_NoBranchSelected_AllBranchesVisibleAndNotHidden()
    {
        // Arrange
        var procedureId = Guid.NewGuid();
        var routerId = Guid.NewGuid();
        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();

        // Simulate: both branch children exist but "Fail" was previously hidden
        // (e.g. from a prior scheduling where "OK" was selected).
        // ManuallySelectedBranch is now null (user cleared it).
        var nodes = new List<Node>
        {
            new RouterNode
            {
                Id = routerId,
                Position = new NodePosition { X = 0, Y = 0 },
                RouterTask = new RouterTask
                {
                    Name = "QualityRouter",
                    StartTime = 0,
                    Duration = 0,
                    Selector = new SimpleVariableSelector { Expression = "QualityOK" },
                    ManuallySelectedBranch = null, // No branch selected — user chose "Choose branch..."
                    Branches = new List<ConditionalBranch>
                    {
                        new()
                        {
                            Name = "OK",
                            Condition = "QualityOK == true",
                            Priority = 1,
                            TargetNodeId = branchAId
                        },
                        new()
                        {
                            Name = "Fail",
                            Condition = "QualityOK == false",
                            Priority = 2,
                            TargetNodeId = branchBId
                        }
                    }
                },
                ProcedureId = default
            },
            new TaskNode
            {
                Id = branchAId,
                ParentId = routerId,
                Extent = "parent",
                Position = new NodePosition { X = 0, Y = 30 },
                Hidden = false,
                Task = new Freydis.Domain.Entities.Procedure.Task
                {
                    Name = "OK Branch",
                    StartTime = 0,
                    Duration = 200
                },
                ProcedureId = default
            },
            new TaskNode
            {
                Id = branchBId,
                ParentId = routerId,
                Extent = "parent",
                Position = new NodePosition { X = 0, Y = 100 },
                Hidden = true, // Stale hidden state from a previous selection
                Task = new Freydis.Domain.Entities.Procedure.Task
                {
                    Name = "Fail Branch",
                    StartTime = 0,
                    Duration = 200
                },
                ProcedureId = default
            }
        };

        var request = new SchedulingRequest
        {
            ProcedureId = procedureId,
            Nodes = nodes,
            Edges = new List<DependencyEdge>(),
            CurrentTime = 0,
            StrictMode = false,
            RouterSelections = null // Design-time
        };

        // Act
        var result = await _orchestrator.CalculateAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue($"but got error: {result.ErrorMessage}");
        result.UpdatedNodes.Should().NotBeNull();

        // Both branches should be visible when no branch is selected
        var branchANode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchAId);
        branchANode.Should().NotBeNull("Branch 'OK' should be in the result");
        branchANode!.Hidden.Should().BeFalse("Branch 'OK' should be visible when nothing selected");

        var branchBNode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == branchBId);
        branchBNode.Should().NotBeNull("Branch 'Fail' should be in the result");
        branchBNode!.Hidden.Should().BeFalse(
            "Branch 'Fail' was previously hidden but must be un-hidden when no branch is selected");

        // Router should be visible
        var routerNode = result.UpdatedNodes!.FirstOrDefault(n => n.Id == routerId);
        routerNode.Should().NotBeNull("Router should be in the result");
        routerNode!.Hidden.Should().BeFalse("Router should be visible");
    }
}