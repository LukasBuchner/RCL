using System.Reactive.Linq;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FHOOE.Freydis.Agents.Tests;

/// <summary>
///     Characterizes how <see cref="DummyRuntimeAgent" /> generates skill <em>output</em> values, the
///     behavior a variable-driven router depends on. The dummy does not perform a real inspection: it
///     synthesizes each output purely from the output property's <see cref="ValueType" />, ignoring the
///     property's configured value for the known primitive types. A boolean output is a simulated
///     quality check (true with high probability), a string output is a fabricated placeholder, and a
///     number output is a random magnitude. These tests pin that contract down so a procedure that routes
///     on an inspected value is modeled against what the mock can actually produce (a boolean quality
///     flag, not a literal string such as "OK").
/// </summary>
public class DummyRuntimeAgentOutputGenerationTests
{
    /// <summary>
    ///     Builds a single-skill <see cref="DummyRuntimeAgent" />, executes the skill to completion, and
    ///     returns the generated outputs from the final progress emission.
    /// </summary>
    /// <param name="skill">The skill to execute; its output properties drive output generation.</param>
    /// <param name="seed">Optional seed making the agent's random draws reproducible.</param>
    /// <param name="outputConfig">Optional output configuration; null preserves the default simulation behavior.</param>
    /// <returns>The generated output map, keyed by output-property name, or null when none were produced.</returns>
    private static async Task<Dictionary<string, object>?> ExecuteAndGetOutputsAsync(
        Skill skill,
        int? seed = null,
        DummyRuntimeAgentOutputConfig? outputConfig = null)
    {
        var pacing = seed is { } s ? DummyRuntimeAgentPacingConfig.Default with { RandomSeed = s } : null;
        var agent = new DummyRuntimeAgent(
            Guid.NewGuid(),
            "TestAgent",
            [skill],
            new Mock<ILogger<DummyRuntimeAgent>>().Object,
            pacing,
            outputConfig);

        // Awaiting the observable yields its final emission, which carries the generated outputs.
        var finalProgress = await agent.ExecuteSkillAsync(Guid.NewGuid(), skill, CancellationToken.None);
        return finalProgress.Outputs;
    }

    /// <summary>
    ///     Builds an "inspect quality" skill with a short nominal duration and a single output property of
    ///     the given configured value, mirroring the shape a router selector reads from.
    /// </summary>
    /// <param name="outputName">Name of the output property (the variable the router branches on).</param>
    /// <param name="configuredOutputValue">The value configured on the output property.</param>
    /// <returns>The constructed skill.</returns>
    private static Skill InspectQualitySkill(string outputName, TypedValue configuredOutputValue)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Inspect Quality",
            Description = "Determines whether an inspected item meets quality requirements.",
            Properties =
            [
                new TypedProperty
                {
                    Name = "NominalDuration",
                    Value = TypedValue.Number(0.1),
                    Direction = PropertyDirection.Input
                },
                new TypedProperty
                {
                    Name = outputName,
                    Value = configuredOutputValue,
                    Direction = PropertyDirection.Output
                }
            ]
        };
    }

    [Fact]
    public async Task ExecuteSkillAsync_BooleanOutput_EmitsBooleanQualityFlag()
    {
        var skill = InspectQualitySkill("QualityOK", TypedValue.Boolean(false));

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1);

        Assert.NotNull(outputs);
        Assert.True(outputs!.ContainsKey("QualityOK"));
        // A boolean output is the dummy's simulated quality check: it emits a real bool the router can
        // evaluate against `QualityOK == true`, not the configured value verbatim.
        Assert.IsType<bool>(outputs["QualityOK"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_StringOutput_FabricatesPlaceholderIgnoringConfiguredValue()
    {
        // The configured value is "OK", the literal a string-equality router condition would test for.
        var skill = InspectQualitySkill("quality", TypedValue.Text("OK"));

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1);

        Assert.NotNull(outputs);
        Assert.True(outputs!.ContainsKey("quality"));
        var value = Assert.IsType<string>(outputs["quality"]);
        // The dummy fabricates a timestamp-based placeholder for string outputs and never echoes the
        // configured value, so no string-equality condition (e.g. `quality == "OK"`) can ever match it.
        // This is exactly why a router must branch on a boolean quality flag, not an inspected string.
        Assert.StartsWith("Output_", value);
        Assert.NotEqual("OK", value);
    }

    [Fact]
    public async Task ExecuteSkillAsync_BooleanOutput_SimulatesHighQualityPassRate()
    {
        var trueCount = 0;
        const int runs = 30;

        // Unseeded so the draws vary run to run, characterizing the simulated ~85% pass rate.
        for (var i = 0; i < runs; i++)
        {
            var outputs = await ExecuteAndGetOutputsAsync(InspectQualitySkill("QualityOK", TypedValue.Boolean(false)));
            Assert.NotNull(outputs);
            var flag = Assert.IsType<bool>(outputs!["QualityOK"]);
            if (flag) trueCount++;
        }

        // The check is probabilistic, not a real inspection: a clear majority pass (the dummy's ~85% rate),
        // but the value is not constant-true by construction. The lower bound is far below the 85% mean to
        // keep the test robust against the random draw while still proving "high pass rate, simulated".
        Assert.True(trueCount >= runs / 2, $"Expected a majority of quality checks to pass, got {trueCount}/{runs}.");
    }

    [Fact]
    public async Task ExecuteSkillAsync_EnumOutput_PassesThroughConfiguredValue()
    {
        // An enum is not one of the primitive types the dummy fabricates, so its switch falls through to
        // returning the configured value. Modeling an inspected quality as an enum therefore yields the
        // exact configured outcome ("OK"), the deterministic, router-evaluable value a string cannot give.
        var skill = InspectQualitySkill("quality", TypedValue.Enum(["OK", "NotOK"], "OK"));

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1);

        Assert.NotNull(outputs);
        Assert.Equal("OK", outputs!["quality"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_SameSeed_ProducesReproducibleOutput()
    {
        var first = await ExecuteAndGetOutputsAsync(InspectQualitySkill("QualityOK", TypedValue.Boolean(false)), 7);
        var second = await ExecuteAndGetOutputsAsync(InspectQualitySkill("QualityOK", TypedValue.Boolean(false)), 7);

        Assert.NotNull(first);
        Assert.NotNull(second);
        // Seeding the agent makes the simulated quality outcome reproducible, so a procedure that routes on
        // it (and the convergence benchmark that records the route) is deterministic per seed.
        Assert.Equal(first!["QualityOK"], second!["QualityOK"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_StringOutputWithUseConfiguredValues_EmitsConfiguredValue()
    {
        // With logical mode enabled, a configured string output is honored verbatim, so a router can branch
        // on `quality == "OK"` deterministically. This is the behavior the convergence benchmark relies on.
        var skill = InspectQualitySkill("quality", TypedValue.Text("OK"));
        var config = new DummyRuntimeAgentOutputConfig { UseConfiguredValues = true };

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1, config);

        Assert.NotNull(outputs);
        Assert.Equal("OK", outputs!["quality"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_BooleanOutputWithUseConfiguredValues_EmitsConfiguredValue()
    {
        // Logical mode honors a configured boolean, overriding the simulated pass-rate path entirely.
        var skill = InspectQualitySkill("QualityOK", TypedValue.Boolean(true));
        var config = new DummyRuntimeAgentOutputConfig { UseConfiguredValues = true, BooleanTruePassRate = 0.0 };

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1, config);

        Assert.NotNull(outputs);
        // The configured value (true) is emitted even though the pass rate is 0, proving the configured
        // value wins over the simulation knob when present.
        Assert.Equal(true, outputs!["QualityOK"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_NumberOutputWithUseConfiguredValues_EmitsConfiguredValue()
    {
        // The number path previously always fabricated a random magnitude; logical mode now honors it.
        var skill = InspectQualitySkill("measurement", TypedValue.Number(42.0));
        var config = new DummyRuntimeAgentOutputConfig { UseConfiguredValues = true };

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1, config);

        Assert.NotNull(outputs);
        Assert.Equal(42.0, outputs!["measurement"]);
    }

    [Fact]
    public async Task ExecuteSkillAsync_BooleanPassRateOne_AlwaysEmitsTrue()
    {
        var config = new DummyRuntimeAgentOutputConfig { BooleanTruePassRate = 1.0 };

        for (var i = 0; i < 10; i++)
        {
            var outputs = await ExecuteAndGetOutputsAsync(
                InspectQualitySkill("QualityOK", TypedValue.Boolean(false)), outputConfig: config);
            Assert.NotNull(outputs);
            Assert.Equal(true, outputs!["QualityOK"]);
        }
    }

    [Fact]
    public async Task ExecuteSkillAsync_BooleanPassRateZero_AlwaysEmitsFalse()
    {
        var config = new DummyRuntimeAgentOutputConfig { BooleanTruePassRate = 0.0 };

        for (var i = 0; i < 10; i++)
        {
            var outputs = await ExecuteAndGetOutputsAsync(
                InspectQualitySkill("QualityOK", TypedValue.Boolean(true)), outputConfig: config);
            Assert.NotNull(outputs);
            Assert.Equal(false, outputs!["QualityOK"]);
        }
    }

    [Fact]
    public async Task ExecuteSkillAsync_UseConfiguredValuesWithNullValue_StillSimulates()
    {
        // The fall-through guard: logical mode is honor-when-present. An output property whose configured
        // value is null still falls through to type-based simulation even with UseConfiguredValues set, so a
        // caller cannot enable the flag, leave the value empty, and silently get a fabricated placeholder
        // while believing it is deterministic.
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Inspect Quality",
            Description = "Determines whether an inspected item meets quality requirements.",
            Properties =
            [
                new TypedProperty
                {
                    Name = "NominalDuration",
                    Value = TypedValue.Number(0.1),
                    Direction = PropertyDirection.Input
                },
                new TypedProperty
                {
                    Name = "quality",
                    Value = new TypedValue { Type = new StringType(), Value = null },
                    Direction = PropertyDirection.Output
                }
            ]
        };
        var config = new DummyRuntimeAgentOutputConfig { UseConfiguredValues = true };

        var outputs = await ExecuteAndGetOutputsAsync(skill, 1, config);

        Assert.NotNull(outputs);
        var value = Assert.IsType<string>(outputs!["quality"]);
        Assert.StartsWith("Output_", value);
    }
}