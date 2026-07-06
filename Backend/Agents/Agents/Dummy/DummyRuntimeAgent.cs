using System.Diagnostics;
using System.Globalization;
using FHOOE.Freydis.Agents.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Agents.Agents.Dummy;

public partial class DummyRuntimeAgent(
    Guid id,
    string name,
    IEnumerable<Skill> availableSkills,
    ILogger<DummyRuntimeAgent> logger,
    DummyRuntimeAgentPacingConfig? pacingConfig = null,
    DummyRuntimeAgentOutputConfig? outputConfig = null)
    : IRuntimeAgent
{
    private readonly List<Skill> _availableSkills = availableSkills.ToList();
    private readonly Lock _lastSeenLock = new();

    /// <summary>
    ///     The timing and perturbation settings governing this agent's execution behavior.
    ///     Defaults to <see cref="DummyRuntimeAgentPacingConfig.Default" /> when no configuration is
    ///     supplied, preserving the agent's historical hardcoded behavior.
    /// </summary>
    private readonly DummyRuntimeAgentPacingConfig
        _pacingConfig = pacingConfig ?? DummyRuntimeAgentPacingConfig.Default;

    /// <summary>
    ///     The settings governing how this agent generates skill output values. Defaults to
    ///     <see cref="DummyRuntimeAgentOutputConfig.Default" /> when no configuration is supplied,
    ///     preserving the agent's historical type-based output fabrication.
    /// </summary>
    private readonly DummyRuntimeAgentOutputConfig
        _outputConfig = outputConfig ?? DummyRuntimeAgentOutputConfig.Default;

    private readonly Random _random = (pacingConfig ?? DummyRuntimeAgentPacingConfig.Default).RandomSeed is { } seed
        ? new Random(seed)
        : new Random();

    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private volatile int _activeExecutions;
    private volatile int _failedExecutions;
    private DateTime _lastSeenUtc = DateTime.UtcNow;
    private volatile int _totalExecutions;

    public Guid Id { get; } = id;
    public string Name { get; } = name;

    public Task<AgentHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        lock (_lastSeenLock)
        {
            _lastSeenUtc = DateTime.UtcNow;
        }

        // Simulate some health metrics
        var process = Process.GetCurrentProcess();
        var cpuUsage = _random.NextDouble() * 15; // Simulate 0-15% CPU usage
        var memoryUsageMb = process.WorkingSet64 / 1024.0 / 1024.0;

        var uptime = DateTime.UtcNow - _startedUtc;
        var averageExecutionTime = _totalExecutions > 0 ? uptime.TotalSeconds / _totalExecutions : 0;

        var healthStatus = new AgentHealthStatus
        {
            AgentId = Id,
            AgentName = Name,
            IsHealthy = true,
            IsAvailable = _activeExecutions < 5,
            ActiveExecutions = _activeExecutions,
            TotalExecutionsCompleted = _totalExecutions,
            FailedExecutions = _failedExecutions,
            LastSeenUtc = GetLastSeenUtc(),
            StartedUtc = _startedUtc,
            CpuUsagePercent = cpuUsage,
            MemoryUsageMb = memoryUsageMb,
            AverageExecutionTimeSeconds = averageExecutionTime,
            StatusMessage = _activeExecutions > 0
                ? $"Executing {_activeExecutions} skill(s)"
                : "Idle - ready for work",
            ErrorMessage = null,
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["SuccessRate"] = _totalExecutions > 0
                    ? (double)(_totalExecutions - _failedExecutions) / _totalExecutions * 100
                    : 100,
                ["UptimeHours"] = uptime.TotalHours,
                ["AvailableSkillsCount"] = _availableSkills.Count,
                ["MaxConcurrentExecutions"] = 5
            }
        };

        return Task.FromResult(healthStatus);
    }

    public Task<IReadOnlyList<Skill>> GetAvailableSkillsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Skill>>(_availableSkills.AsReadOnly());
    }

    public virtual Task<SkillExecutionEstimate?> GetExecutionEstimateAsync(Skill skill,
        CancellationToken cancellationToken = default)
    {
        logger.LogDummyEstimatingExecution(Name, skill.Name, skill.Id);

        if (_availableSkills.All(s => s.Id != skill.Id))
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogSkillNotAvailable(
                    Name, skill.Name, skill.Id,
                    string.Join(", ", _availableSkills.Select(s => $"{s.Name} ({s.Id})")));
            return Task.FromResult<SkillExecutionEstimate?>(null);
        }

        var canAdapt = skill.Name.Contains("Adaptive", StringComparison.OrdinalIgnoreCase);
        logger.LogSkillCanAdapt(skill.Name, canAdapt);

        // Log all skill properties for debugging
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogSkillProperties(
                skill.Name, skill.Properties.Count,
                string.Join(", ", skill.Properties.Select(p => $"{p.Name}={GetPropertyValueString(p.Value)}")));

        var nominalDurationProperty = skill.Properties.FirstOrDefault(p => p.Name == "NominalDuration");
        double nominalDuration;

        if (nominalDurationProperty?.Value is { Type: NumberType, Value: double durValue })
        {
            nominalDuration = durValue;
            logger.LogNominalDurationFound(skill.Name, nominalDuration);
        }
        else
        {
            nominalDuration = _random.Next(8, 12);
            logger.LogNominalDurationMissing(skill.Name, nominalDuration, nominalDurationProperty != null);
        }

        double? minAdaptive = null;

        if (!canAdapt)
        {
            var fixedEstimate = new SkillExecutionEstimate
            {
                Skill = skill,
                AgentId = Id,
                CanExecuteAdaptively = canAdapt,
                EstimatedNominalDuration = nominalDuration,
                MinAdaptiveDuration = minAdaptive
            };

            logger.LogFixedEstimateCreated(Name, skill.Name, nominalDuration, canAdapt);

            return Task.FromResult<SkillExecutionEstimate?>(fixedEstimate);
        }

        minAdaptive = nominalDuration * 0.75;

        var adaptiveEstimate = new SkillExecutionEstimate
        {
            Skill = skill,
            AgentId = Id,
            CanExecuteAdaptively = canAdapt,
            EstimatedNominalDuration = nominalDuration,
            MinAdaptiveDuration = minAdaptive
        };

        logger.LogAdaptiveEstimateCreated(Name, skill.Name, nominalDuration, minAdaptive);

        return Task.FromResult<SkillExecutionEstimate?>(adaptiveEstimate);
    }

    public virtual Task<bool> CanExecuteAdaptivelyAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        // First check if the agent has this skill
        if (_availableSkills.All(s => s.Id != skill.Id))
            return Task.FromResult(false);

        return Task.FromResult(skill.Name.Contains("Adaptive", StringComparison.OrdinalIgnoreCase));
    }

    // Moved to DummyRuntimeAgentExecuteSkillAsync.cs partial class file

    private DateTime GetLastSeenUtc()
    {
        lock (_lastSeenLock)
        {
            return _lastSeenUtc;
        }
    }

    private void UpdateLastSeenUtc()
    {
        lock (_lastSeenLock)
        {
            _lastSeenUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     Helper method to convert property values to readable strings for logging.
    /// </summary>
    private static string GetPropertyValueString(object propertyValue)
    {
        if (propertyValue is not TypedValue typedValue)
            return propertyValue?.ToString() ?? "null";

        return typedValue.Type switch
        {
            NumberType when typedValue.Value is double d => d.ToString(CultureInfo.InvariantCulture),
            StringType when typedValue.Value is string s => $"'{s}'",
            BooleanType when typedValue.Value is bool b => b.ToString(),
            _ => typedValue.Value?.ToString() ?? "null"
        };
    }

    /// <summary>
    ///     Generates output values for a skill execution from the skill's output properties. The value for
    ///     each property is produced by <see cref="GenerateOutputValue" />, which either honors the
    ///     property's configured value or synthesizes one from its type depending on the agent's
    ///     <see cref="DummyRuntimeAgentOutputConfig" />.
    /// </summary>
    /// <param name="skill">The skill that was executed.</param>
    /// <returns>
    ///     A dictionary mapping output property names to their generated values,
    ///     or null if the skill has no output properties.
    /// </returns>
    private Dictionary<string, object>? GenerateSkillOutputs(Skill skill)
    {
        // Find all output properties (Output or InputOutput direction)
        var outputProperties = skill.Properties
            .Where(p => p.Direction == PropertyDirection.Output || p.Direction == PropertyDirection.InputOutput)
            .ToList();

        if (outputProperties.Count == 0)
            return null;

        var outputs = new Dictionary<string, object>();

        foreach (var property in outputProperties)
        {
            var outputValue = GenerateOutputValue(property);

            outputs[property.Name] = outputValue;

            logger.LogGeneratedOutput(skill.Name, property.Name, outputValue);
        }

        return outputs;
    }

    /// <summary>
    ///     Produces the output value for a single output property. When the agent's
    ///     <see cref="DummyRuntimeAgentOutputConfig.UseConfiguredValues" /> is set and the property carries a
    ///     non-null configured value, that value is emitted verbatim so downstream consumers (for example a
    ///     router branching on a quality flag) see a deterministic, evaluable result. Otherwise the value is
    ///     synthesized from the property's type, reproducing the agent's historical simulation: a boolean is
    ///     a simulated quality check that passes with the configured
    ///     <see cref="DummyRuntimeAgentOutputConfig.BooleanTruePassRate" />, a number is a random magnitude,
    ///     and a string is a fabricated timestamp-based placeholder.
    /// </summary>
    /// <param name="property">The output property whose value is being produced.</param>
    /// <returns>The generated output value (boxed).</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a non-primitive output property carries no configured value to fall back to.
    /// </exception>
    private object GenerateOutputValue(TypedProperty property)
    {
        // Logical mode: honor the configured value verbatim when present, so the output is deterministic and
        // router-evaluable. A null configured value falls through to type-based simulation below.
        if (_outputConfig.UseConfiguredValues && property.Value.Value is { } configured)
            return configured;

        // Simulation mode: synthesize from the property type (the agent's historical behavior).
        return property.Value.Type switch
        {
            // A single Next(100) draw against the pass rate scaled by 100. Default 0.85 * 100 == 85.0, so
            // this is identical to the historical `Next(100) < 85`; 1.0 always passes, 0.0 never does.
            BooleanType => _random.Next(100) < _outputConfig.BooleanTruePassRate * 100,
            NumberType => _random.NextDouble() * 100, // Random number 0-100
            StringType => $"Output_{DateTime.UtcNow.Ticks}", // Timestamp-based string
            _ => property.Value.Value ?? throw new InvalidOperationException(
                $"Unable to generate output for property '{property.Name}' of type '{property.Value.Type.TypeName}'")
        };
    }
}