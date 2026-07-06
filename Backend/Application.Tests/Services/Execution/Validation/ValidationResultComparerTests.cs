using FHOOE.Freydis.Application.Services.Execution.Validation;
using FluentAssertions;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Validation;

/// <summary>
///     Unit tests for <see cref="ValidationResultComparer" />.
///     These tests verify that the comparer performs deep equality checks on the actual skill node
///     IDs and missing FS pair IDs — not just collection counts — so that
///     <c>DistinctUntilChanged</c> correctly emits when the same violation counts appear with
///     different underlying identifiers.
/// </summary>
public sealed class ValidationResultComparerTests
{
    private readonly ValidationResultComparer _comparer = ValidationResultComparer.Instance;

    // -------------------------------------------------------------------------
    // Baseline positive case
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Two results carrying identical agent IDs, skill node IDs, and pair IDs must be
    ///     considered equal so that <c>DistinctUntilChanged</c> suppresses the redundant emission.
    /// </summary>
    [Fact]
    public void Equals_IdenticalContent_ReturnsTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();

        var violation = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeA, nodeB)]);

        var x = BuildResult([violation]);
        var y = BuildResult([violation]);

        // Act & Assert
        _comparer.Equals(x, y).Should().BeTrue(
            "two results with identical agent IDs, skill node IDs, and pair IDs are structurally equal");
    }

    // -------------------------------------------------------------------------
    // Same counts, different unserialized skill IDs → must differ
    // -------------------------------------------------------------------------

    /// <summary>
    ///     When two violations share the same agent and collection counts but contain different
    ///     unserialized skill node IDs, the comparer must return <see langword="false" />.
    ///     This is the core regression case: swapping which skill is unserialized while keeping
    ///     the count unchanged must not suppress the downstream emission.
    /// </summary>
    [Fact]
    public void Equals_SameCounts_DifferentSkillIds_ReturnsFalse()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var nodeC = Guid.NewGuid(); // different third node

        // Same agent, same count (2 unserialized skills), but different node IDs
        var violationX = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeA, nodeB)]);

        var violationY = BuildViolation(agentId,
            [nodeA, nodeC], // nodeC replaces nodeB
            [(nodeA, nodeC)]);

        var x = BuildResult([violationX]);
        var y = BuildResult([violationY]);

        // Act & Assert
        _comparer.Equals(x, y).Should().BeFalse(
            "swapping which skill node is unserialized must not be suppressed by DistinctUntilChanged");
    }

    // -------------------------------------------------------------------------
    // Same counts, different pair IDs → must differ
    // -------------------------------------------------------------------------

    /// <summary>
    ///     When two violations share the same agent and collection counts but list different
    ///     missing FS pair IDs, the comparer must return <see langword="false" />.
    ///     A swap of which pair is missing is a semantically different validation result.
    /// </summary>
    [Fact]
    public void Equals_SameCounts_DifferentPairIds_ReturnsFalse()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var nodeC = Guid.NewGuid();
        var nodeD = Guid.NewGuid();

        // Both violations have two unserialized skills and one missing pair, but different pair members
        var violationX = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeA, nodeB)]);

        var violationY = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeC, nodeD)]); // completely different pair

        var x = BuildResult([violationX]);
        var y = BuildResult([violationY]);

        // Act & Assert
        _comparer.Equals(x, y).Should().BeFalse(
            "different missing FS pair IDs represent a distinct validation result even when counts match");
    }

    // -------------------------------------------------------------------------
    // Pair list order independence
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Two violations that list the same missing FS pairs in different positions within the
    ///     list must be considered equal, because <see cref="ValidationResultComparer" /> treats
    ///     the pair list as an unordered set.
    /// </summary>
    [Fact]
    public void Equals_PairsInDifferentOrder_ReturnsTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var nodeC = Guid.NewGuid();
        var nodeD = Guid.NewGuid();

        var violationX = BuildViolation(agentId,
            [nodeA, nodeB, nodeC, nodeD],
            [(nodeA, nodeB), (nodeC, nodeD)]); // order: AB first

        var violationY = BuildViolation(agentId,
            [nodeA, nodeB, nodeC, nodeD],
            [(nodeC, nodeD), (nodeA, nodeB)]); // order: CD first

        var x = BuildResult([violationX]);
        var y = BuildResult([violationY]);

        // Act & Assert
        _comparer.Equals(x, y).Should().BeTrue(
            "the pair list is treated as an unordered set so list order must not affect equality");
    }

    // -------------------------------------------------------------------------
    // Intra-pair member swap (canonical ordering)
    // -------------------------------------------------------------------------

    /// <summary>
    ///     A <see cref="SkillPair" /> with members <c>(A, B)</c> must compare equal to a pair
    ///     with members <c>(B, A)</c> because the pair is unordered.  The comparer normalizes
    ///     each pair by placing the smaller <see cref="Guid" /> first before set comparison.
    /// </summary>
    [Fact]
    public void Equals_PairsWithInternalSwap_ReturnsTrue()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();

        var violationX = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeA, nodeB)]); // SkillA = nodeA, SkillB = nodeB

        var violationY = BuildViolation(agentId,
            [nodeA, nodeB],
            [(nodeB, nodeA)]); // SkillA = nodeB, SkillB = nodeA — swapped inside the pair

        var x = BuildResult([violationX]);
        var y = BuildResult([violationY]);

        // Act & Assert
        _comparer.Equals(x, y).Should().BeTrue(
            "(A,B) and (B,A) represent the same missing FS edge and must be considered equal");
    }

    // -------------------------------------------------------------------------
    // Private builder helpers
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Constructs an <see cref="AgentSerializationViolation" /> from flat ID lists.
    /// </summary>
    /// <param name="agentId">The agent the violation belongs to.</param>
    /// <param name="unserializedIds">Node IDs of the unserialized skills.</param>
    /// <param name="pairs">Raw <c>(SkillA, SkillB)</c> tuples for missing FS pairs.</param>
    /// <returns>A fully populated <see cref="AgentSerializationViolation" />.</returns>
    private static AgentSerializationViolation BuildViolation(
        Guid agentId,
        Guid[] unserializedIds,
        (Guid SkillA, Guid SkillB)[] pairs)
    {
        return new AgentSerializationViolation
        {
            AgentId = agentId,
            AgentName = $"Agent {agentId}",
            UnserializedSkills = unserializedIds
                .Select(id => new UnserializedSkill(id, $"Skill {id}"))
                .ToList(),
            MissingFsPairs = pairs
                .Select(t => new SkillPair(t.SkillA, t.SkillB))
                .ToList()
        };
    }

    /// <summary>
    ///     Wraps a list of violations inside a <see cref="ProcedureValidationResult" />.
    /// </summary>
    /// <param name="violations">The violations to include.</param>
    /// <returns>A <see cref="ProcedureValidationResult" /> containing the given violations.</returns>
    private static ProcedureValidationResult BuildResult(
        IReadOnlyList<AgentSerializationViolation> violations)
    {
        return new ProcedureValidationResult { AgentSerializationViolations = violations };
    }
}