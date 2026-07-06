namespace FHOOE.Freydis.Agents.Agents.Dummy;

/// <summary>
///     Configures how a <see cref="DummyRuntimeAgent" /> generates the <em>output values</em> of a skill
///     execution. This is distinct from <see cref="DummyRuntimeAgentPacingConfig" />, which governs timing
///     and perturbation: this type governs <em>what value</em> a completed skill emits for each of its
///     output properties. Every field carries the default that reproduces the agent's historical hardcoded
///     output behavior, so an agent constructed without an explicit configuration emits outputs exactly as
///     before this type existed.
/// </summary>
/// <remarks>
///     <para>
///         The boolean and number paths draw from the agent's random number generator, which is seeded
///         through <see cref="DummyRuntimeAgentPacingConfig.RandomSeed" />. This type therefore consumes,
///         but does not own, that seed: to make a boolean or number output reproducible, set the seed on the
///         pacing configuration. To make an output fully deterministic regardless of seeding, use
///         <see cref="UseConfiguredValues" />.
///     </para>
///     <para>
///         <see cref="UseConfiguredValues" /> applies per agent, not per property: it is all-or-nothing for
///         that agent's skills. An agent cannot simulate one output while honoring a configured value for
///         another in the same run.
///     </para>
/// </remarks>
public sealed record DummyRuntimeAgentOutputConfig
{
    /// <summary>
    ///     The default output configuration. All fields take the values that the
    ///     <see cref="DummyRuntimeAgent" /> previously hardcoded, so this is behaviorally identical to the
    ///     agent's original output generation. Used as the fallback whenever no configuration is supplied to
    ///     the agent's constructor.
    /// </summary>
    public static DummyRuntimeAgentOutputConfig Default { get; } = new();

    /// <summary>
    ///     Whether to emit an output property's configured value verbatim instead of synthesizing one from
    ///     its type.
    ///     <para>
    ///         This is <strong>honor-when-present</strong>, not a blanket "all outputs are deterministic"
    ///         switch: a value is honored only for an output property whose configured value is non-null.
    ///         An output property with a <c>null</c> configured value still falls through to type-based
    ///         simulation (for a string output, the fabricated placeholder), even when this is <c>true</c>.
    ///         A caller that sets this flag but leaves an output property's value empty therefore still
    ///         receives a simulated value, not a deterministic one.
    ///     </para>
    ///     <para>
    ///         When <c>false</c> (the default), every output is synthesized from its type, reproducing the
    ///         agent's historical behavior. When <c>true</c>, a router or downstream consumer can branch on
    ///         the exact configured output (for example a quality flag set to a known value).
    ///     </para>
    /// </summary>
    public bool UseConfiguredValues { get; init; }

    /// <summary>
    ///     The probability, in the range <c>[0, 1]</c>, that a boolean output is emitted as <c>true</c> when
    ///     it is synthesized (that is, in simulation mode). This models a quality check that passes with a
    ///     given rate. A value of <c>0.85</c> reproduces the historical 85% pass rate; <c>1.0</c> always
    ///     emits <c>true</c> and <c>0.0</c> always emits <c>false</c>.
    ///     <para>
    ///         The implementation compares a single <c>Random.Next(100)</c> draw against this rate scaled by
    ///         100, so the boundary is integral. The three calibrated values (<c>0.85</c>, <c>1.0</c>,
    ///         <c>0.0</c>) land on exact integer boundaries; an arbitrary rate is rounded to the nearest
    ///         percent by that comparison.
    ///     </para>
    /// </summary>
    public double BooleanTruePassRate { get; init; } = 0.85;
}