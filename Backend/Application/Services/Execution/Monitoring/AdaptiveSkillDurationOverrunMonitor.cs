using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Execution.Monitoring;

/// <summary>
///     Observability-only monitor that watches executing skills against their scheduled finish
///     and raises an advisory when one overruns by a configurable margin. It reads two read-only
///     observability streams — <see cref="INodeChangeTracker.Nodes" /> for each skill's scheduled
///     finish and run-status, and <see cref="IExecutionTimingPublisher" /> as the clock — and writes
///     only to <see cref="IExecutionAdvisoryPublisher" /> and the log. It takes no dependency on the
///     execution event bus, so it structurally cannot publish to, gate, or trigger the control
///     plane; information flows control → observability only.
/// </summary>
public sealed class AdaptiveSkillDurationOverrunMonitor : IHostedService, IDisposable
{
    private readonly IExecutionAdvisoryPublisher _advisoryPublisher;
    private readonly AdaptiveSkillMonitoringConfiguration _config;
    private readonly ILogger<AdaptiveSkillDurationOverrunMonitor> _logger;
    private readonly INodeChangeTracker _nodeChangeTracker;

    /// <summary>Highest advisory severity already raised per executing skill node, for debounce.</summary>
    private readonly ConcurrentDictionary<Guid, ExecutionAdvisorySeverity> _raised = new();

    private readonly IExecutionTimingPublisher _timingPublisher;
    private IDisposable? _subscription;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdaptiveSkillDurationOverrunMonitor" /> class.
    /// </summary>
    /// <param name="nodeChangeTracker">Read-only source of per-skill scheduled finish and run-status.</param>
    /// <param name="timingPublisher">Read-only source of the procedure-relative clock.</param>
    /// <param name="advisoryPublisher">Side channel for operator-facing advisories.</param>
    /// <param name="options">Monitor configuration (margins, enablement).</param>
    /// <param name="logger">Logger for advisories and handler errors.</param>
    public AdaptiveSkillDurationOverrunMonitor(
        INodeChangeTracker nodeChangeTracker,
        IExecutionTimingPublisher timingPublisher,
        IExecutionAdvisoryPublisher advisoryPublisher,
        IOptions<AdaptiveSkillMonitoringConfiguration> options,
        ILogger<AdaptiveSkillDurationOverrunMonitor> logger)
    {
        _nodeChangeTracker = nodeChangeTracker ?? throw new ArgumentNullException(nameof(nodeChangeTracker));
        _timingPublisher = timingPublisher ?? throw new ArgumentNullException(nameof(timingPublisher));
        _advisoryPublisher = advisoryPublisher ?? throw new ArgumentNullException(nameof(advisoryPublisher));
        _config = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Releases the stream subscription.
    /// </summary>
    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
            return Task.CompletedTask;

        // Combine the per-skill scheduled-finish snapshots with the clock. ObserveOn a worker so a
        // slow advisory/log handler can never backpressure the execution pipeline that feeds the
        // source subjects.
        _subscription = _nodeChangeTracker.Nodes
            .CombineLatest(_timingPublisher.TimingUpdates,
                (nodes, timing) => (nodes, timing.CurrentTimeSeconds))
            .ObserveOn(Scheduler.Default)
            .Subscribe(pair => Evaluate(pair.nodes, pair.CurrentTimeSeconds));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Evaluates the current node snapshot against the clock and raises advisories for skills
    ///     that have overrun their scheduled finish. Observability only: it never publishes to the
    ///     execution bus or alters any task. Errors are caught and logged so they cannot fault the
    ///     source stream.
    /// </summary>
    /// <param name="nodes">The latest node snapshot.</param>
    /// <param name="currentTimeSeconds">The current procedure-relative time, in seconds.</param>
    internal void Evaluate(IReadOnlyList<Node> nodes, double currentTimeSeconds)
    {
        try
        {
            var executingIds = new HashSet<Guid>();

            foreach (var node in nodes)
            {
                if (node is not SkillExecutionNode skillNode)
                    continue;

                var task = skillNode.SkillExecutionTask;
                if (task.IsExecuting != true || !task.FinishTime.HasValue)
                    continue;

                executingIds.Add(node.Id);

                var overrun = currentTimeSeconds - task.FinishTime.Value;
                if (overrun <= _config.OverrunMarginSeconds)
                    continue;

                var severity = overrun > _config.EscalateMarginSeconds
                    ? ExecutionAdvisorySeverity.Escalated
                    : ExecutionAdvisorySeverity.Warning;

                if (_raised.TryGetValue(node.Id, out var alreadyRaised) && alreadyRaised >= severity)
                    continue;

                _raised[node.Id] = severity;
                RaiseAdvisory(node.Id, task.Name, overrun, task.FinishTime.Value, severity);
            }

            // Drop debounce state for skills that are no longer executing so a future run can warn again.
            foreach (var staleId in _raised.Keys.Where(id => !executingIds.Contains(id)).ToList())
                _raised.TryRemove(staleId, out _);
        }
        catch (Exception ex)
        {
            _logger.LogAdaptiveSkillMonitorError(ex);
        }
    }

    private void RaiseAdvisory(
        Guid skillId,
        string skillName,
        double overrunSeconds,
        double scheduledFinishSeconds,
        ExecutionAdvisorySeverity severity)
    {
        if (severity == ExecutionAdvisorySeverity.Escalated)
            _logger.LogAdaptiveSkillOverrunEscalated(skillName, skillId, overrunSeconds, scheduledFinishSeconds);
        else
            _logger.LogAdaptiveSkillOverrun(skillName, skillId, overrunSeconds, scheduledFinishSeconds);

        var message = severity == ExecutionAdvisorySeverity.Escalated
            ? $"Skill '{skillName}' has run {overrunSeconds:F0}s past its scheduled finish and is still executing — operator attention recommended."
            : $"Skill '{skillName}' has run {overrunSeconds:F0}s past its scheduled finish — check whether an upstream task or the operator is blocked.";

        _advisoryPublisher.Publish(new ExecutionAdvisory
        {
            SkillId = skillId,
            SkillName = skillName,
            OverrunSeconds = overrunSeconds,
            ScheduledFinishSeconds = scheduledFinishSeconds,
            Severity = severity,
            Message = message,
            RaisedAtUtc = DateTimeOffset.UtcNow
        });
    }
}