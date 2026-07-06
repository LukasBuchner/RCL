using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Agents.Tests;

/// <summary>
///     Layer-1 integration tests for <see cref="DummyRuntimeAgent.CanExecuteAdaptivelyAsync" />.
///     Constructs a real <see cref="DummyRuntimeAgent" /> (no behavioural mocks) and exercises
///     the adaptive-capability predicate against canonical skill names so the validator's
///     Rule 10 stands on a verified contract for this agent type.
/// </summary>
public class DummyRuntimeAgentAdaptivePredicateTests
{
    private readonly Mock<ILogger<DummyRuntimeAgent>> _logger = new();

    /// <summary>
    ///     Verifies that the canonical "Move To Position" skill — present in the agent's
    ///     available-skills list — is reported as non-adaptive because the substring
    ///     "Adaptive" is absent from the skill name.
    /// </summary>
    [Fact]
    public async Task CanExecuteAdaptivelyAsync_MoveToPositionInAvailableSkills_ReturnsFalse()
    {
        var skill = MakeSkill("Move To Position");
        var agent = MakeAgent("Bob", [skill]);

        var canAdapt = await agent.CanExecuteAdaptivelyAsync(skill);

        Assert.False(canAdapt);
    }

    /// <summary>
    ///     Verifies the substring rule: a skill named "Move To Position Adaptive" — present
    ///     in the agent's available-skills list — is reported as adaptive.
    /// </summary>
    [Fact]
    public async Task CanExecuteAdaptivelyAsync_NameContainsAdaptiveAndInAvailableSkills_ReturnsTrue()
    {
        var skill = MakeSkill("Move To Position Adaptive");
        var agent = MakeAgent("Bob", [skill]);

        var canAdapt = await agent.CanExecuteAdaptivelyAsync(skill);

        Assert.True(canAdapt);
    }

    /// <summary>
    ///     Verifies the substring rule is case-insensitive.
    /// </summary>
    [Fact]
    public async Task CanExecuteAdaptivelyAsync_NameContainsAdaptiveCaseInsensitive_ReturnsTrue()
    {
        var skill = MakeSkill("MOVE TO POSITION ADAPTIVE");
        var agent = MakeAgent("Bob", [skill]);

        var canAdapt = await agent.CanExecuteAdaptivelyAsync(skill);

        Assert.True(canAdapt);
    }

    /// <summary>
    ///     Verifies that a skill not present in the agent's available-skills list is
    ///     reported as non-adaptive regardless of its name (skill-list membership is the
    ///     first gate before the substring check).
    /// </summary>
    [Fact]
    public async Task CanExecuteAdaptivelyAsync_SkillNotInAvailableSkillsEvenIfNameSaysAdaptive_ReturnsFalse()
    {
        var skill = MakeSkill("Move To Position Adaptive");
        var agent = MakeAgent("Bob", []);

        var canAdapt = await agent.CanExecuteAdaptivelyAsync(skill);

        Assert.False(canAdapt);
    }

    /// <summary>
    ///     Verifies that "Hold Position" — adaptive on the Digital Twin agent — is reported
    ///     as non-adaptive on the Dummy agent because the Dummy uses a name-based contract,
    ///     not a property-based one. Documents the per-agent-type contract for future
    ///     readers.
    /// </summary>
    [Fact]
    public async Task CanExecuteAdaptivelyAsync_HoldPositionOnDummyAgent_ReturnsFalse()
    {
        var skill = MakeSkill("Hold Position");
        var agent = MakeAgent("Bob", [skill]);

        var canAdapt = await agent.CanExecuteAdaptivelyAsync(skill);

        Assert.False(canAdapt);
    }

    private DummyRuntimeAgent MakeAgent(string name, IEnumerable<Skill> availableSkills)
    {
        return new DummyRuntimeAgent(
            Guid.NewGuid(),
            name,
            availableSkills,
            _logger.Object);
    }

    private static Skill MakeSkill(string name)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test",
            Properties = []
        };
    }
}