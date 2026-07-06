using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Execution.StateManagement;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.StateManagement;

/// <summary>
///     Unit tests for SkillExecutionStateManager service.
/// </summary>
public class SkillExecutionStateManagerTests
{
    private readonly SkillExecutionStateManager _stateManager = new(NullLogger<SkillExecutionStateManager>.Instance);

    [Fact]
    public void Initialize_WithValidNodesAndAgents_CreatesStates()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var nodes = new List<Node> { skillNode1, skillNode2 }.AsReadOnly();

        var agent1 = CreateMockAgent("Agent 1");
        var agent2 = CreateMockAgent("Agent 2");
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode1.Id, agent1 },
            { skillNode2.Id, agent2 }
        };

        // Act
        _stateManager.Initialize(nodes, agentAssignments);

        // Assert
        var state1 = _stateManager.GetState(skillNode1.Id);
        var state2 = _stateManager.GetState(skillNode2.Id);

        Assert.NotNull(state1);
        Assert.NotNull(state2);
        Assert.Equal(ExecutionStatus.NotStarted, state1.ExecutionStatus);
        Assert.Equal(ExecutionStatus.NotStarted, state2.ExecutionStatus);
    }

    [Fact]
    public void Initialize_WithOnlySkillNodes_IgnoresOtherNodeTypes()
    {
        // Arrange
        var taskNode = CreateTaskNode("Task 1");
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { taskNode, skillNode }.AsReadOnly();

        var agent = CreateMockAgent("Agent 1");
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode.Id, agent }
        };

        // Act
        _stateManager.Initialize(nodes, agentAssignments);

        // Assert
        var allStates = _stateManager.GetAllStates();
        Assert.Single(allStates); // Only skill node should have state
        Assert.NotNull(_stateManager.GetState(skillNode.Id));
        Assert.Null(_stateManager.GetState(taskNode.Id)); // Task node should not have state
    }

    [Fact]
    public void Initialize_EmptyTaskBetweenSkills_TracksOnlyTheTwoSkills()
    {
        // A -> Task(empty) -> B. The empty task fires as a leafless endpoint at runtime but has no skill-like
        // state, so completion tracking covers only the two skills; the empty task neither blocks completion
        // nor counts toward it.
        var skillA = CreateSkillExecutionNode("A");
        var emptyTask = CreateTaskNode("EmptyTask");
        var skillB = CreateSkillExecutionNode("B");
        var nodes = new List<Node> { skillA, emptyTask, skillB }.AsReadOnly();

        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillA.Id, CreateMockAgent("AgentA") },
            { skillB.Id, CreateMockAgent("AgentB") }
        };

        // Act
        _stateManager.Initialize(nodes, agentAssignments);

        // Assert — only the two skills are tracked; the empty task is not.
        var allStates = _stateManager.GetAllStates();
        Assert.Equal(2, allStates.Count);
        Assert.NotNull(_stateManager.GetState(skillA.Id));
        Assert.NotNull(_stateManager.GetState(skillB.Id));
        Assert.Null(_stateManager.GetState(emptyTask.Id));
    }

    [Fact]
    public void Initialize_StoresAgentAssignments()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();

        var agent = CreateMockAgent("Agent 1");
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode.Id, agent }
        };

        // Act
        _stateManager.Initialize(nodes, agentAssignments);

        // Assert
        var assignedAgent = _stateManager.GetAssignedAgent(skillNode.Id);
        Assert.NotNull(assignedAgent);
        Assert.Equal(agent, assignedAgent);
    }

    [Fact]
    public void Initialize_WithNullNodes_ThrowsArgumentNullException()
    {
        // Arrange
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _stateManager.Initialize(null!, agentAssignments));
    }

    [Fact]
    public void Initialize_WithNullAgentAssignments_ThrowsArgumentNullException()
    {
        // Arrange
        var nodes = new List<Node>().AsReadOnly();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _stateManager.Initialize(nodes, null!));
    }

    [Fact]
    public void Initialize_CalledTwice_ClearsPreviousState()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var nodes1 = new List<Node> { skillNode1 }.AsReadOnly();
        var agentAssignments1 = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode1.Id, CreateMockAgent("Agent 1") }
        };

        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var nodes2 = new List<Node> { skillNode2 }.AsReadOnly();
        var agentAssignments2 = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode2.Id, CreateMockAgent("Agent 2") }
        };

        // Act
        _stateManager.Initialize(nodes1, agentAssignments1);
        _stateManager.Initialize(nodes2, agentAssignments2);

        // Assert
        Assert.Null(_stateManager.GetState(skillNode1.Id)); // First node should be cleared
        Assert.NotNull(_stateManager.GetState(skillNode2.Id)); // Second node should exist
    }

    [Fact]
    public void GetState_WithValidId_ReturnsState()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var state = _stateManager.GetState(skillNode.Id);

        // Assert
        Assert.NotNull(state);
        Assert.Equal(skillNode.Id, state.SkillNode.Id);
    }

    [Fact]
    public void GetState_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var state = _stateManager.GetState(Guid.NewGuid());

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void GetAllStates_ReturnsAllInitializedStates()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var skillNode3 = CreateSkillExecutionNode("Skill 3");
        var nodes = new List<Node> { skillNode1, skillNode2, skillNode3 }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var allStates = _stateManager.GetAllStates();

        // Assert
        Assert.Equal(3, allStates.Count);
        Assert.Contains(allStates, s => s.SkillNode.Id == skillNode1.Id);
        Assert.Contains(allStates, s => s.SkillNode.Id == skillNode2.Id);
        Assert.Contains(allStates, s => s.SkillNode.Id == skillNode3.Id);
    }

    [Fact]
    public void GetStatesByStatus_ReturnsCorrectStates()
    {
        // Arrange
        var skillNode1 = CreateSkillExecutionNode("Skill 1");
        var skillNode2 = CreateSkillExecutionNode("Skill 2");
        var skillNode3 = CreateSkillExecutionNode("Skill 3");
        var nodes = new List<Node> { skillNode1, skillNode2, skillNode3 }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Update states
        _stateManager.UpdateState(skillNode1.Id, s => s.ExecutionStatus = ExecutionStatus.Running);
        _stateManager.UpdateState(skillNode2.Id, s => s.ExecutionStatus = ExecutionStatus.Running);
        _stateManager.UpdateState(skillNode3.Id, s => s.ExecutionStatus = ExecutionStatus.Completed);

        // Act
        var runningStates = _stateManager.GetStatesByStatus(ExecutionStatus.Running);
        var completedStates = _stateManager.GetStatesByStatus(ExecutionStatus.Completed);

        // Assert
        Assert.Equal(2, runningStates.Count);
        Assert.Single(completedStates);
        Assert.Contains(runningStates, s => s.SkillNode.Id == skillNode1.Id);
        Assert.Contains(runningStates, s => s.SkillNode.Id == skillNode2.Id);
        Assert.Contains(completedStates, s => s.SkillNode.Id == skillNode3.Id);
    }

    [Fact]
    public void UpdateState_WithValidId_UpdatesState()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        _stateManager.UpdateState(skillNode.Id, state =>
        {
            state.ExecutionStatus = ExecutionStatus.Running;
            state.LastProgressPercentage = 50.0;
        });

        // Assert
        var state = _stateManager.GetState(skillNode.Id);
        Assert.NotNull(state);
        Assert.Equal(ExecutionStatus.Running, state.ExecutionStatus);
        Assert.Equal(50.0, state.LastProgressPercentage);
    }

    [Fact]
    public void UpdateState_WithInvalidId_DoesNothing()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act - should not throw
        _stateManager.UpdateState(Guid.NewGuid(), state => { state.ExecutionStatus = ExecutionStatus.Running; });

        // Assert - original state unchanged
        var state = _stateManager.GetState(skillNode.Id);
        Assert.Equal(ExecutionStatus.NotStarted, state!.ExecutionStatus);
    }

    [Fact]
    public void UpdateState_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _stateManager.UpdateState(skillNode.Id, null!));
    }

    [Fact]
    public void GetAssignedAgent_WithValidAssignment_ReturnsAgent()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();

        var agent = CreateMockAgent("Agent 1");
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>
        {
            { skillNode.Id, agent }
        };

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var assignedAgent = _stateManager.GetAssignedAgent(skillNode.Id);

        // Assert
        Assert.NotNull(assignedAgent);
        Assert.Equal(agent, assignedAgent);
    }

    [Fact]
    public void GetAssignedAgent_WithNoAssignment_ReturnsNull()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>(); // No assignment for this skill

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var assignedAgent = _stateManager.GetAssignedAgent(skillNode.Id);

        // Assert
        Assert.Null(assignedAgent);
    }

    [Fact]
    public void GetAssignedAgent_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var skillNode = CreateSkillExecutionNode("Skill 1");
        var nodes = new List<Node> { skillNode }.AsReadOnly();
        var agentAssignments = new Dictionary<Guid, IRuntimeAgent>();

        _stateManager.Initialize(nodes, agentAssignments);

        // Act
        var assignedAgent = _stateManager.GetAssignedAgent(Guid.NewGuid());

        // Assert
        Assert.Null(assignedAgent);
    }

    #region Terminal-state guard (monotone transitions)

    /// <summary>
    ///     Drives a skill to <see cref="ExecutionStatus.Completed" /> and asserts that a
    ///     subsequent <see cref="ISkillExecutionStateManager.UpdateState" /> with a lambda
    ///     trying to set the status back to <see cref="ExecutionStatus.Running" /> is
    ///     rejected; the status remains <see cref="ExecutionStatus.Completed" />.
    /// </summary>
    [Fact]
    public void UpdateState_FromCompleted_NoOps()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Completed);
        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Running);

        Assert.Equal(ExecutionStatus.Completed, _stateManager.GetState(skillNode.Id)!.ExecutionStatus);
    }

    /// <summary>
    ///     Verifies that <see cref="ExecutionStatus.Failed" /> is absorbing: a subsequent
    ///     lambda cannot rewrite the status.
    /// </summary>
    [Fact]
    public void UpdateState_FromFailed_NoOps()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Failed);
        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Running);

        Assert.Equal(ExecutionStatus.Failed, _stateManager.GetState(skillNode.Id)!.ExecutionStatus);
    }

    /// <summary>
    ///     Verifies that <see cref="ExecutionStatus.NotSelected" /> is absorbing.
    /// </summary>
    [Fact]
    public void UpdateState_FromNotSelected_NoOps()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.NotSelected);
        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Running);

        Assert.Equal(ExecutionStatus.NotSelected, _stateManager.GetState(skillNode.Id)!.ExecutionStatus);
    }

    /// <summary>
    ///     Verifies that a late progress event arriving after the skill has reached
    ///     <see cref="ExecutionStatus.Completed" /> does not update the progress
    ///     fields. The audit's primary failure mode: <c>UpdateProgress</c> writing
    ///     stale data into a terminal state.
    /// </summary>
    [Fact]
    public void UpdateState_LateProgressAfterCompleted_DoesNotUpdateProgressFields()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        _stateManager.UpdateState(skillNode.Id, s =>
        {
            s.ExecutionStatus = ExecutionStatus.Running;
            s.LastProgressPercentage = 50.0;
        });
        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Completed);

        _stateManager.UpdateState(skillNode.Id, s => { s.LastProgressPercentage = 100.0; });

        var state = _stateManager.GetState(skillNode.Id)!;
        Assert.Equal(ExecutionStatus.Completed, state.ExecutionStatus);
        Assert.Equal(50.0, state.LastProgressPercentage);
    }

    /// <summary>
    ///     Guardrail: the new terminal-state guard must not break the happy path of
    ///     <c>NotStarted → Running → Completed</c>.
    /// </summary>
    [Fact]
    public void UpdateState_NonTerminalTransitions_StillSucceed()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Running);
        Assert.Equal(ExecutionStatus.Running, _stateManager.GetState(skillNode.Id)!.ExecutionStatus);

        _stateManager.UpdateState(skillNode.Id, s => s.ExecutionStatus = ExecutionStatus.Completed);
        Assert.Equal(ExecutionStatus.Completed, _stateManager.GetState(skillNode.Id)!.ExecutionStatus);
    }

    /// <summary>
    ///     Verifies that a duplicate "mark completed" call preserves the original
    ///     <c>CompletedAt</c> timestamp rather than overwriting it.
    /// </summary>
    [Fact]
    public void UpdateState_DoubleCompleted_KeepsOriginalCompletedAt()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        var firstCompletedAt = DateTimeOffset.UtcNow;
        _stateManager.UpdateState(skillNode.Id, s =>
        {
            s.ExecutionStatus = ExecutionStatus.Completed;
            s.CompletedAt = firstCompletedAt;
        });

        var secondCompletedAt = firstCompletedAt.AddSeconds(10);
        _stateManager.UpdateState(skillNode.Id, s => { s.CompletedAt = secondCompletedAt; });

        Assert.Equal(firstCompletedAt, _stateManager.GetState(skillNode.Id)!.CompletedAt);
    }

    /// <summary>
    ///     Concurrent <see cref="ExecutionStatus.Completed" /> and
    ///     <see cref="ExecutionStatus.Failed" /> transitions must produce a single
    ///     terminal status; only one wins and the loser is rejected.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateState_ConcurrentTerminalTransitions_OnlyOneWins()
    {
        var skillNode = CreateSkillExecutionNode("Skill 1");
        _stateManager.Initialize([skillNode], new Dictionary<Guid, IRuntimeAgent>());

        var start = new ManualResetEventSlim(false);
        var markCompleted = System.Threading.Tasks.Task.Run(() =>
        {
            start.Wait();
            _stateManager.UpdateState(skillNode.Id, s =>
            {
                s.ExecutionStatus = ExecutionStatus.Completed;
                s.CompletedAt = DateTimeOffset.UtcNow;
            });
        });
        var markFailed = System.Threading.Tasks.Task.Run(() =>
        {
            start.Wait();
            _stateManager.UpdateState(skillNode.Id, s =>
            {
                s.ExecutionStatus = ExecutionStatus.Failed;
                s.ErrorMessage = "boom";
                s.CompletedAt = DateTimeOffset.UtcNow;
            });
        });

        start.Set();
        await System.Threading.Tasks.Task.WhenAll(markCompleted, markFailed);

        var state = _stateManager.GetState(skillNode.Id)!;
        Assert.True(state.ExecutionStatus is ExecutionStatus.Completed or ExecutionStatus.Failed);
        // Whichever lambda won set CompletedAt; the loser is rejected, so the state
        // is internally consistent: terminal status with a CompletedAt timestamp.
        Assert.NotNull(state.CompletedAt);
        if (state.ExecutionStatus == ExecutionStatus.Failed)
            Assert.Equal("boom", state.ErrorMessage);
        else
            Assert.Null(state.ErrorMessage);
    }

    #endregion

    // Helper methods
    private static TaskNode CreateTaskNode(string name, Guid? parentId = null)
    {
        return new TaskNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            Task = new Task
            {
                Name = name,
                Description = $"Test task: {name}",
                StartTime = 0.0,
                Duration = 10.0,
                FinishTime = 10.0
            },
            ProcedureId = default
        };
    }

    private static SkillExecutionNode CreateSkillExecutionNode(string skillName, Guid? parentId = null)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Position = new NodePosition
            {
                X = 0,
                Y = 0
            },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = skillName,
                Duration = 5.0,
                StartTime = 0.0,
                FinishTime = 5.0,
                AgentId = Guid.NewGuid(),
                Skill = CreateSkill(skillName)
            },
            ProcedureId = default
        };
    }

    private static Skill CreateSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test skill",
            Properties = new List<TypedProperty>()
        };
    }

    private static IRuntimeAgent CreateMockAgent(string name)
    {
        var mock = new Mock<IRuntimeAgent>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.Id).Returns(Guid.NewGuid());
        return mock.Object;
    }
}