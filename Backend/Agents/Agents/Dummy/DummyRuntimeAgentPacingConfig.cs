namespace FHOOE.Freydis.Agents.Agents.Dummy;

/// <summary>
///     Configures the timing and perturbation behavior of a <see cref="DummyRuntimeAgent" />.
///     Every field carries the default that reproduces the agent's historical hardcoded behavior,
///     so an agent constructed without an explicit configuration behaves identically to before this
///     type existed. Supplying a non-default configuration lets a caller (for example a headless
///     convergence benchmark) shape how the mock agent paces its work and how much random variation
///     it introduces, and optionally make those random draws reproducible via a fixed seed.
/// </summary>
public sealed record DummyRuntimeAgentPacingConfig
{
    /// <summary>
    ///     The default pacing configuration. All fields take the values that the
    ///     <see cref="DummyRuntimeAgent" /> previously hardcoded, so this is behaviorally identical
    ///     to the agent's original implementation. Used as the fallback whenever no configuration is
    ///     supplied to the agent's constructor.
    /// </summary>
    public static DummyRuntimeAgentPacingConfig Default { get; } = new();

    /// <summary>
    ///     The symmetric fractional jitter applied to a fixed-duration skill's nominal duration to
    ///     simulate real-world execution variability. A value of <c>0.15</c> draws the actual
    ///     duration uniformly from the range <c>[nominal * 0.85, nominal * 1.15]</c>, that is
    ///     ±15% around the nominal duration. A value of <c>0</c> disables jitter entirely.
    /// </summary>
    public double DurationJitter { get; init; } = 0.15;

    /// <summary>
    ///     The peak amplitude, as a fraction of the nominal duration, of the simulated estimate noise
    ///     applied during fixed-duration execution. The effective amplitude decays linearly from this
    ///     value to zero as the skill nears completion, so early estimates oscillate around the true
    ///     duration and then converge. A value of <c>0.03</c> reproduces the historical 3% amplitude.
    /// </summary>
    public double EstimateNoiseAmplitude { get; init; } = 0.03;

    /// <summary>
    ///     The angular frequency (in radians per elapsed second) of the sinusoid that drives the
    ///     simulated estimate noise during fixed-duration execution. A value of <c>0.3</c> reproduces
    ///     the historical oscillation frequency.
    /// </summary>
    public double EstimateSinusoidFrequency { get; init; } = 0.3;

    /// <summary>
    ///     The interval, in milliseconds, between progress emissions of the reactive execution timer
    ///     used by both fixed-duration and adaptive execution. A value of <c>3</c> reproduces the
    ///     historical 3 ms tick. Smaller values emit progress more frequently at the cost of more work.
    /// </summary>
    public int TimerTickMs { get; init; } = 3;

    /// <summary>
    ///     An optional seed for the agent's random number generator. When set, the agent's random draws
    ///     (duration jitter, health metrics, and the simulated boolean and number skill outputs) become
    ///     reproducible across runs that share the seed. When <c>null</c> (the default), the agent uses a
    ///     non-deterministic, time-based seed, matching the historical behavior.
    ///     <para>
    ///         Two things the seed does <em>not</em> govern: progress timing is still driven by wall-clock
    ///         time, so the seed reproduces the random draws, not the absolute execution timing; and a
    ///         simulated <em>string</em> output is a wall-clock timestamp (<c>DateTime.UtcNow.Ticks</c>),
    ///         which is not seed-reproducible. To make an output <em>value</em> deterministic regardless of
    ///         seeding, set <see cref="DummyRuntimeAgentOutputConfig.UseConfiguredValues" /> and configure
    ///         the output property's value.
    ///     </para>
    /// </summary>
    public int? RandomSeed { get; init; }
}