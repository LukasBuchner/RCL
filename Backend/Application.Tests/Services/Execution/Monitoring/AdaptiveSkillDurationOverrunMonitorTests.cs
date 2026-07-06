using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Events;
using FHOOE.Freydis.Application.Services.Execution.Monitoring;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Monitoring;

/// <summary>
///     Tests for the observability-only <see cref="AdaptiveSkillDurationOverrunMonitor" />. They drive
///     the monitor's evaluation directly with controlled node snapshots and a clock value, asserting
///     it raises advisories on overrun, stays silent during normal stretching, debounces, escalates,
///     and never faults — while remaining structurally isolated from the control-plane event bus.
/// </summary>
public class AdaptiveSkillDurationOverrunMonitorTests
{
    private const double OverrunMargin = 30.0;
    private const double EscalateMargin = 120.0;

    /// <summary>
    ///     The monitor must not depend on the control-plane event bus: if it never holds an
    ///     <see cref="ISkillExecutionEventBus" />, it structurally cannot publish to, gate, or
    ///     trigger the control plane. This is the proof-faithfulness guard.
    /// </summary>
    [Fact]
    public void Constructor_DoesNotDependOnExecutionEventBus()
    {
        var ctor = Assert.Single(typeof(AdaptiveSkillDurationOverrunMonitor).GetConstructors());
        Assert.DoesNotContain(ctor.GetParameters(), p => p.ParameterType == typeof(ISkillExecutionEventBus));
    }

    /// <summary>
    ///     A skill that has run past its scheduled finish by more than the margin raises exactly one
    ///     warning advisory.
    /// </summary>
    [Fact]
    public void Evaluate_OverrunBeyondMargin_RaisesWarningAdvisory()
    {
        var publisher = new RecordingAdvisoryPublisher();
        var logger = new CapturingLogger<AdaptiveSkillDurationOverrunMonitor>();
        var monitor = CreateMonitor(publisher, logger);
        var skillId = Guid.NewGuid();
        var nodes = new List<Node> { ExecutingSkillNode(skillId, "Weld", 50.0) };

        monitor.Evaluate(nodes, 50.0 + OverrunMargin + 1.0);

        var advisory = Assert.Single(publisher.Published);
        Assert.Equal(skillId, advisory.SkillId);
        Assert.Equal(ExecutionAdvisorySeverity.Warning, advisory.Severity);
        Assert.True(advisory.OverrunSeconds > OverrunMargin);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    /// <summary>
    ///     A skill still within the overrun margin of its scheduled finish — the normal adaptive
    ///     stretching this whole design exists to support — must not raise any advisory.
    /// </summary>
    [Fact]
    public void Evaluate_WithinMargin_RaisesNothing()
    {
        var publisher = new RecordingAdvisoryPublisher();
        var monitor = CreateMonitor(publisher);
        var nodes = new List<Node> { ExecutingSkillNode(Guid.NewGuid(), "Weld", 50.0) };

        monitor.Evaluate(nodes, 50.0 + OverrunMargin - 1.0);

        Assert.Empty(publisher.Published);
    }

    /// <summary>
    ///     A skill that is no longer executing raises nothing even if the clock is well past its finish.
    /// </summary>
    [Fact]
    public void Evaluate_NotExecuting_RaisesNothing()
    {
        var publisher = new RecordingAdvisoryPublisher();
        var monitor = CreateMonitor(publisher);
        var nodes = new List<Node> { ExecutingSkillNode(Guid.NewGuid(), "Weld", 50.0, false) };

        monitor.Evaluate(nodes, 50.0 + EscalateMargin + 100.0);

        Assert.Empty(publisher.Published);
    }

    /// <summary>
    ///     A sustained overrun at the same severity raises only one advisory across repeated evaluations.
    /// </summary>
    [Fact]
    public void Evaluate_SustainedOverrun_DebouncesToSingleWarning()
    {
        var publisher = new RecordingAdvisoryPublisher();
        var monitor = CreateMonitor(publisher);
        var skillId = Guid.NewGuid();
        var nodes = new List<Node> { ExecutingSkillNode(skillId, "Weld", 50.0) };

        monitor.Evaluate(nodes, 50.0 + OverrunMargin + 1.0);
        monitor.Evaluate(nodes, 50.0 + OverrunMargin + 2.0);
        monitor.Evaluate(nodes, 50.0 + OverrunMargin + 3.0);

        Assert.Single(publisher.Published);
    }

    /// <summary>
    ///     When a warned skill crosses the escalation margin, a second, escalated advisory is raised.
    /// </summary>
    [Fact]
    public void Evaluate_OverrunCrossesEscalationMargin_RaisesEscalatedAdvisory()
    {
        var publisher = new RecordingAdvisoryPublisher();
        var monitor = CreateMonitor(publisher);
        var skillId = Guid.NewGuid();
        var nodes = new List<Node> { ExecutingSkillNode(skillId, "Weld", 50.0) };

        monitor.Evaluate(nodes, 50.0 + OverrunMargin + 1.0);
        monitor.Evaluate(nodes, 50.0 + EscalateMargin + 1.0);

        Assert.Equal(2, publisher.Published.Count);
        Assert.Equal(ExecutionAdvisorySeverity.Warning, publisher.Published[0].Severity);
        Assert.Equal(ExecutionAdvisorySeverity.Escalated, publisher.Published[1].Severity);
    }

    /// <summary>
    ///     An exception thrown by the advisory side channel is caught and logged, never propagated to
    ///     the source stream.
    /// </summary>
    [Fact]
    public void Evaluate_AdvisoryPublisherThrows_IsCaughtAndLogged()
    {
        var logger = new CapturingLogger<AdaptiveSkillDurationOverrunMonitor>();
        var monitor = CreateMonitor(new ThrowingAdvisoryPublisher(), logger);
        var nodes = new List<Node> { ExecutingSkillNode(Guid.NewGuid(), "Weld", 50.0) };

        var exception = Record.Exception(() => monitor.Evaluate(nodes, 50.0 + OverrunMargin + 1.0));

        Assert.Null(exception);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    private static AdaptiveSkillDurationOverrunMonitor CreateMonitor(
        IExecutionAdvisoryPublisher advisoryPublisher,
        ILogger<AdaptiveSkillDurationOverrunMonitor>? logger = null)
    {
        var config = Options.Create(new AdaptiveSkillMonitoringConfiguration
        {
            Enabled = true,
            OverrunMarginSeconds = OverrunMargin,
            EscalateMarginSeconds = EscalateMargin
        });

        return new AdaptiveSkillDurationOverrunMonitor(
            new Mock<INodeChangeTracker>().Object,
            new Mock<IExecutionTimingPublisher>().Object,
            advisoryPublisher,
            config,
            logger ?? new CapturingLogger<AdaptiveSkillDurationOverrunMonitor>());
    }

    private static SkillExecutionNode ExecutingSkillNode(
        Guid id, string name, double finishTime, bool isExecuting = true)
    {
        return new SkillExecutionNode
        {
            Id = id,
            ProcedureId = Guid.NewGuid(),
            Position = new NodePosition { X = 0, Y = 0 },
            SkillExecutionTask = new SkillExecutionTask
            {
                Name = name,
                StartTime = 0.0,
                Duration = finishTime,
                FinishTime = finishTime,
                IsExecuting = isExecuting,
                AgentId = Guid.NewGuid(),
                Skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = name,
                    Properties = new List<TypedProperty>()
                }
            }
        };
    }

    private sealed class RecordingAdvisoryPublisher : IExecutionAdvisoryPublisher
    {
        private readonly Subject<ExecutionAdvisory> _subject = new();
        public List<ExecutionAdvisory> Published { get; } = new();
        public IObservable<ExecutionAdvisory> Advisories => _subject;

        public void Publish(ExecutionAdvisory advisory)
        {
            Published.Add(advisory);
            _subject.OnNext(advisory);
        }
    }

    private sealed class ThrowingAdvisoryPublisher : IExecutionAdvisoryPublisher
    {
        public IObservable<ExecutionAdvisory> Advisories => new Subject<ExecutionAdvisory>();

        public void Publish(ExecutionAdvisory advisory)
        {
            throw new InvalidOperationException("advisory channel failure");
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoopScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NoopScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}