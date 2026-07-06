using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Validation;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Validation;

/// <summary>
///     Integration-style unit tests for <see cref="ProcedureValidationTracker" />.
///     These tests exercise the reactive pipeline (CombineLatest → Throttle → DistinctUntilChanged)
///     using real time with short delays because the tracker's <c>Throttle</c> operator uses the
///     default Rx scheduler and is not injectable.  Assertions wait 1.5–2 s past the throttle
///     window (1 s) to avoid flakiness on slow CI machines.
/// </summary>
public sealed class ProcedureValidationTrackerTests : IDisposable
{
    private readonly BehaviorSubject<IReadOnlyList<Node>> _nodesSubject;
    private readonly BehaviorSubject<IReadOnlyList<DependencyEdge>> _edgesSubject;
    private readonly Mock<INodeChangeTracker> _mockNodeTracker;
    private readonly Mock<IDependencyEdgeChangeTracker> _mockEdgeTracker;
    private readonly Mock<IAgentSerializationValidator> _mockValidator;
    private readonly ProcedureValidationTracker _tracker;

    /// <summary>
    ///     Initializes the reactive subjects and wires them up to the mocked change trackers
    ///     before constructing the system under test.
    /// </summary>
    public ProcedureValidationTrackerTests()
    {
        _nodesSubject = new BehaviorSubject<IReadOnlyList<Node>>([]);
        _edgesSubject = new BehaviorSubject<IReadOnlyList<DependencyEdge>>([]);

        _mockNodeTracker = new Mock<INodeChangeTracker>();
        _mockNodeTracker.Setup(t => t.Nodes).Returns(_nodesSubject.AsObservable());

        _mockEdgeTracker = new Mock<IDependencyEdgeChangeTracker>();
        _mockEdgeTracker.Setup(t => t.Edges).Returns(_edgesSubject.AsObservable());

        _mockValidator = new Mock<IAgentSerializationValidator>();

        // Default: no violations
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns([]);

        _tracker = new ProcedureValidationTracker(
            _mockNodeTracker.Object,
            _mockEdgeTracker.Object,
            _mockValidator.Object,
            NullLogger<ProcedureValidationTracker>.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _tracker.Dispose();
    }

    // -------------------------------------------------------------------------
    // Test 19: Emits on violation change
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Test 19: When the graph initially has a same-agent conflict and then an FS edge is
    ///     added to resolve it, the tracker must emit two distinct results via the BehaviorSubject:
    ///     first a result with one violation, then a result with no violations.
    ///     The <c>DistinctUntilChanged</c> operator suppresses any subsequent identical emissions.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task EmitsOnViolationChange_TwoDistinctEmissions()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);

        var violation = new AgentSerializationViolation
        {
            AgentId = agentId,
            AgentName = $"Agent {agentId}",
            UnserializedSkills = [new UnserializedSkill(skillA.Id, "A"), new UnserializedSkill(skillB.Id, "B")],
            MissingFsPairs = [new SkillPair(skillA.Id, skillB.Id)]
        };

        // When there are no edges → violation
        _mockValidator
            .Setup(v => v.Validate(
                It.Is<IReadOnlyList<Node>>(n => n.Count == 2),
                It.Is<IReadOnlyList<DependencyEdge>>(e => e.Count == 0)))
            .Returns([violation]);

        // When there is an FS edge → no violations
        _mockValidator
            .Setup(v => v.Validate(
                It.Is<IReadOnlyList<Node>>(n => n.Count == 2),
                It.Is<IReadOnlyList<DependencyEdge>>(e => e.Count == 1)))
            .Returns([]);

        var violationCounts = new List<int>();
        using var subscription = _tracker.ValidationResults
            .Subscribe(r => violationCounts.Add(r.AgentSerializationViolations.Count));

        // Act — push the conflicting state (2 skills, 0 edges)
        _nodesSubject.OnNext([skillA, skillB]);
        _edgesSubject.OnNext([]);

        // Wait past the 1 s throttle window so the first result is forwarded
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        // Resolve the conflict by adding the FS edge (2 skills, 1 edge)
        _edgesSubject.OnNext([CreateFsEdge(skillA.Id, skillB.Id)]);

        // Wait past the throttle window again for the second result
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        // Assert
        // Emissions: [0] initial BehaviorSubject value (empty); [1] violation result; [2] resolved result.
        // We need at least the violation emission (count > 0) followed by a clean emission (count == 0).
        violationCounts.Should().Contain(1, "a same-agent conflict must produce a violation emission");
        var violationIndex = violationCounts.LastIndexOf(1);
        violationCounts.Skip(violationIndex + 1).Should().Contain(0,
            "after adding the FS edge the tracker must emit a clean result");
    }

    // -------------------------------------------------------------------------
    // Test 20: DistinctUntilChanged suppresses redundant emissions
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Test 20: When the graph emits a position-only node update (same agent assignments, same
    ///     edges, same validation outcome), the <c>DistinctUntilChanged</c> operator must suppress
    ///     the second emission so the subscriber does not receive a redundant update.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task PositionOnlyUpdate_SuppressesRedundantEmission()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);

        // The validator always returns empty (single skill, no pairs to check).
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns([]);

        var emitted = new List<ProcedureValidationResult>();
        using var subscription = _tracker.ValidationResults.Subscribe(r => emitted.Add(r));

        // Push first update — triggers the pipeline after the throttle window.
        _nodesSubject.OnNext([skillA]);
        _edgesSubject.OnNext([]);
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        var countAfterFirstUpdate = emitted.Count;

        // Act — push a second update that produces the exact same validation result.
        // This simulates a position-only change where only NodePosition values change.
        var skillAMoved = skillA with { Position = new NodePosition { X = 99, Y = 99 } };
        _nodesSubject.OnNext([skillAMoved]);
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        // Assert
        // DistinctUntilChanged should suppress the second emission because the validation
        // outcome (no violations) is structurally identical to the previous result.
        emitted.Count.Should().Be(countAfterFirstUpdate,
            "a position-only change that does not alter validation outcome must not produce a new emission");
    }

    // -------------------------------------------------------------------------
    // Test 21: Throttle coalesces rapid successive updates
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Test 21: Verifies that rapid successive updates within the throttle window are coalesced
    ///     so that only one new result is forwarded to downstream subscribers.
    ///     <para>
    ///         The setup ensures the validation outcome actually changes (from no-violation to violation)
    ///         on each push so that <c>DistinctUntilChanged</c> does not suppress the single coalesced
    ///         emission.  Five pushes are made within ~250 ms; only the last value in the quiet window
    ///         should reach the subscriber, yielding at most 2 downstream emissions (1 is ideal; 2 is
    ///         acceptable to tolerate boundary timing on slow CI).
    ///     </para>
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RapidUpdates_ThrottleCoalescesIntoSingleEmission()
    {
        // Arrange — baseline: no violations
        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns([]);

        // Let the initial BehaviorSubject value settle through the pipeline.
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        // Now switch the validator to always return a violation so that each rapid push produces
        // a result that differs from the baseline.  This prevents DistinctUntilChanged from masking
        // the coalesced emission (which we need to count).
        var agentId = Guid.NewGuid();
        var skillA = CreateSkillNode("A", agentId);
        var skillB = CreateSkillNode("B", agentId);
        var singleViolation = new AgentSerializationViolation
        {
            AgentId = agentId,
            AgentName = $"Agent {agentId}",
            UnserializedSkills = [new UnserializedSkill(skillA.Id, "A"), new UnserializedSkill(skillB.Id, "B")],
            MissingFsPairs = [new SkillPair(skillA.Id, skillB.Id)]
        };

        _mockValidator
            .Setup(v => v.Validate(It.IsAny<IReadOnlyList<Node>>(), It.IsAny<IReadOnlyList<DependencyEdge>>()))
            .Returns([singleViolation]);

        // Subscribe *after* resetting the validator so we start counting from this point.
        var downstreamEmissions = 0;
        using var subscription = _tracker.ValidationResults
            .Skip(1) // skip the BehaviorSubject's synchronous replay of the current (clean) value
            .Subscribe(_ => downstreamEmissions++);

        // Act — push 5 node snapshots within ~250 ms (well within the 1 s throttle window)
        for (var i = 0; i < 5; i++)
        {
            _nodesSubject.OnNext([skillA, skillB]);
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        // Wait past the throttle window for the coalesced emission to reach subscribers.
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1.5));

        // Assert
        // Throttle(1 s) should coalesce all 5 pushes (within ~250 ms) into a single downstream
        // emission.  We allow up to 2 to tolerate minor timing variance on slow CI.
        downstreamEmissions.Should().BeGreaterThan(0,
            "the tracker must emit at least one result after the rapid updates settle");
        downstreamEmissions.Should().BeLessThanOrEqualTo(2,
            "five updates within 250 ms must be coalesced by the 1 s throttle into at most one or two emissions");
    }

    // -------------------------------------------------------------------------
    // Private helper methods
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Creates a <see cref="SkillExecutionNode" /> with the given display name and agent assignment.
    /// </summary>
    /// <param name="name">Human-readable name used as both task name and skill name.</param>
    /// <param name="agentId">The agent identifier assigned to execute this skill.</param>
    /// <returns>A fully initialized <see cref="SkillExecutionNode" />.</returns>
    private static SkillExecutionNode CreateSkillNode(string name, Guid agentId)
    {
        return new SkillExecutionNode
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0,
                Duration = 1,
                AgentId = agentId,
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = string.Empty,
                    Properties = []
                }
            }
        };
    }

    /// <summary>
    ///     Creates a Finish-to-Start <see cref="DependencyEdge" /> using the canonical handle
    ///     strings ("right" source = Finish, "left" target = Start).
    /// </summary>
    /// <param name="sourceId">The ID of the predecessor node.</param>
    /// <param name="targetId">The ID of the successor node.</param>
    /// <returns>A <see cref="DependencyEdge" /> representing an FS dependency.</returns>
    private static DependencyEdge CreateFsEdge(Guid sourceId, Guid targetId)
    {
        return new DependencyEdge
        {
            Id = Guid.NewGuid(),
            ProcedureId = Guid.NewGuid(),
            SourceId = sourceId,
            TargetId = targetId,
            SourceHandle = "right", // Finish
            TargetHandle = "left" // Start
        };
    }
}