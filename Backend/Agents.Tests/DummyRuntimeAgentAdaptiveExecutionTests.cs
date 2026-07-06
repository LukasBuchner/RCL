using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Agents.Dummy;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace FHOOE.Freydis.Agents.Tests;

/// <summary>
///     Tests specifically for the ExecuteSkillAdaptivelyAsync method to ensure
///     the pure Rx.NET implementation works correctly.
/// </summary>
public class DummyRuntimeAgentAdaptiveExecutionTests : IDisposable
{
    private readonly Skill _adaptiveSkill;
    private readonly DummyRuntimeAgent _agent;
    private readonly Mock<ILogger<DummyRuntimeAgent>> _mockLogger;
    private readonly ITestOutputHelper _output;

    /// <summary>
    ///     Non-zero while the test is active, cleared in <see cref="Dispose" />. Rx
    ///     subscriptions in these tests run on background schedulers and may deliver a
    ///     final emission just after the test method returns; <see cref="SafeWriteLine" />
    ///     reads this flag to drop such late diagnostic writes.
    /// </summary>
    private int _testActive = 1;

    public DummyRuntimeAgentAdaptiveExecutionTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<DummyRuntimeAgent>>();

        // Create an adaptive skill for testing
        _adaptiveSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Test Adaptive Skill",
            Description = "Test skill for adaptive execution",
            Properties =
            [
                new TypedProperty
                {
                    Name = "NominalDuration",
                    Value = TypedValue.Number(60),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        // Create agent with the adaptive skill
        _agent = new DummyRuntimeAgent(
            Guid.NewGuid(),
            "TestAgent",
            [_adaptiveSkill],
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ExecuteSkillAdaptivelyAsync_ShouldEmitProgressUpdates()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var plannedFinishTimes = Observable.Never<double>();
        var finishSignal = Observable.Never<Unit>();
        var cts = new CancellationTokenSource();

        var progressUpdates = new List<SkillExecutionProgress>();

        // Act - Subscribe to the execution and collect progress updates
        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                executionId,
                _adaptiveSkill,
                65.0, // initial target duration
                plannedFinishTimes,
                finishSignal,
                cts.Token)
            .Subscribe(
                progress =>
                {
                    progressUpdates.Add(progress);
                    SafeWriteLine($"Progress at {progress.CurrentTimeIntoExecution:F1}s: {progress.StatusMessage}");
                },
                ex => SafeWriteLine($"Error: {ex.Message}"),
                () => SafeWriteLine("Execution completed")
            );

        // Wait for a few progress updates
        await Task.Delay(3500); // Should get ~3 progress updates

        // Cleanup
        subscription.Dispose();
        cts.Cancel();

        // Assert
        Assert.NotEmpty(progressUpdates);
        Assert.True(progressUpdates.Count >= 3, $"Expected at least 3 progress updates, got {progressUpdates.Count}");

        // Verify progress updates are incremental
        for (var i = 1; i < progressUpdates.Count; i++)
            Assert.True(progressUpdates[i].CurrentTimeIntoExecution > progressUpdates[i - 1].CurrentTimeIntoExecution,
                "Progress updates should show increasing time");

        // Verify all progress updates have required fields
        foreach (var progress in progressUpdates)
        {
            Assert.Equal(executionId, progress.ExecutionId);
            Assert.Equal(_adaptiveSkill.Id, progress.SkillId);
            Assert.Equal(_agent.Id, progress.AgentId);
            Assert.NotNull(progress.StatusMessage);
            Assert.True(progress.MinAchievableDuration.HasValue);
        }
    }

    [Fact]
    public async Task ExecuteSkillAdaptivelyAsync_ShouldCompleteViaFinishSignal()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var plannedFinishTimes = Observable.Never<double>();
        var finishSignalSubject = new Subject<Unit>();
        var cts = new CancellationTokenSource();

        var progressUpdates = new List<SkillExecutionProgress>();
        var completed = false;

        // Act - Subscribe to the execution
        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                executionId,
                _adaptiveSkill,
                65.0,
                plannedFinishTimes,
                finishSignalSubject,
                cts.Token)
            .Subscribe(
                progress =>
                {
                    progressUpdates.Add(progress);
                    SafeWriteLine($"Progress at {progress.CurrentTimeIntoExecution:F1}s: {progress.StatusMessage}");
                },
                ex => SafeWriteLine($"Error: {ex.Message}"),
                () =>
                {
                    completed = true;
                    SafeWriteLine("Execution completed");
                }
            );

        // Wait for some progress updates
        await Task.Delay(2500); // Get ~2 progress updates

        // Send finish signal
        SafeWriteLine("Sending finish signal...");
        finishSignalSubject.OnNext(Unit.Default);
        finishSignalSubject.OnCompleted();

        // Wait a bit for completion
        await Task.Delay(500, cts.Token);

        // Cleanup
        subscription.Dispose();
        await cts.CancelAsync();

        // Assert
        Assert.True(completed, "Observable should have completed after finish signal");
        Assert.NotEmpty(progressUpdates);

        // The last progress update should indicate successful completion
        var lastProgress = progressUpdates.Last();
        Assert.True(lastProgress.CompletedSuccessfully, "Last progress should indicate successful completion");
        Assert.Contains("finish signal", lastProgress.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteSkillAdaptivelyAsync_ShouldHandlePlannedFinishTimeUpdates()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var plannedFinishSubject = new Subject<double>();
        var finishSignal = Observable.Never<Unit>();
        var cts = new CancellationTokenSource();

        var progressUpdates = new List<SkillExecutionProgress>();

        // Act - Subscribe to the execution
        var subscription = _agent.ExecuteSkillAdaptivelyAsync(
                executionId,
                _adaptiveSkill,
                65.0,
                plannedFinishSubject,
                finishSignal,
                cts.Token)
            .Subscribe(
                progress =>
                {
                    progressUpdates.Add(progress);
                    SafeWriteLine(
                        $"Progress: Target={progress.EstimatedTotalDuration:F1}s, Current={progress.CurrentTimeIntoExecution:F1}s");
                },
                ex => SafeWriteLine($"Error: {ex.Message}"),
                () => SafeWriteLine("Execution completed")
            );

        // Wait for initial progress
        await Task.Delay(1500);

        // Send updated planned finish time
        SafeWriteLine("Updating planned finish to 50s...");
        plannedFinishSubject.OnNext(50.0);

        await Task.Delay(1000);

        // Send another update
        SafeWriteLine("Updating planned finish to 80s...");
        plannedFinishSubject.OnNext(80.0);

        await Task.Delay(1000);

        // Cleanup
        subscription.Dispose();
        plannedFinishSubject.OnCompleted();
        cts.Cancel();

        // Assert
        Assert.NotEmpty(progressUpdates);

        // Check that EstimatedTotalDuration changes reflect the updates
        // (Note: Due to clamping to min/max, the exact values may differ)
        var distinctTargets = progressUpdates.Select(p => p.EstimatedTotalDuration).Distinct().Count();
        Assert.True(distinctTargets > 1, "Should have seen different target durations after updates");
    }

    [Fact]
    public async Task ExecuteSkillAdaptivelyAsync_ShouldNotSelfTerminate_AndCompleteOnlyOnFinishSignal()
    {
        // Arrange — a short nominal duration confirms the absence of any upper-duration cap:
        // even after elapsing well beyond the nominal pace, execution must keep emitting progress
        // and complete only once the finish signal fires.
        var executionId = Guid.NewGuid();
        var plannedFinishTimes = Observable.Never<double>();
        var finishSignalSubject = new Subject<Unit>();
        var cts = new CancellationTokenSource();

        var shortSkill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Short Adaptive Skill",
            Description = "Short skill for unbounded-duration testing",
            Properties =
            [
                new TypedProperty
                {
                    Name = "NominalDuration",
                    Value = TypedValue.Number(1),
                    Direction = PropertyDirection.Input
                }
            ]
        };

        var agent = new DummyRuntimeAgent(
            Guid.NewGuid(),
            "TestAgent",
            [shortSkill],
            _mockLogger.Object
        );

        var progressUpdates = new List<SkillExecutionProgress>();
        var completed = false;

        // Act
        var subscription = agent.ExecuteSkillAdaptivelyAsync(
                executionId,
                shortSkill,
                1.0,
                plannedFinishTimes,
                finishSignalSubject,
                cts.Token)
            .Subscribe(
                progress =>
                {
                    progressUpdates.Add(progress);
                    SafeWriteLine($"Progress at {progress.CurrentTimeIntoExecution:F1}s: {progress.StatusMessage}");
                },
                ex => SafeWriteLine($"Error: {ex.Message}"),
                () =>
                {
                    completed = true;
                    SafeWriteLine("Execution completed");
                }
            );

        // Elapse well past the nominal duration without a finish signal.
        await Task.Delay(2500);

        // The execution must still be running (no self-termination at any upper bound).
        Assert.False(completed, "Execution must not self-terminate without a finish signal");
        var emittedBeforeSignal = progressUpdates.Count;
        Assert.True(emittedBeforeSignal >= 3,
            $"Expected ongoing progress emissions, got {emittedBeforeSignal}");
        Assert.All(progressUpdates, p => Assert.False(p.CompletedSuccessfully));

        // Now drive the finish signal — only this should complete execution.
        SafeWriteLine("Sending finish signal...");
        finishSignalSubject.OnNext(Unit.Default);
        finishSignalSubject.OnCompleted();

        await Task.Delay(500);

        // Cleanup
        subscription.Dispose();
        cts.Cancel();

        // Assert
        Assert.True(completed, "Execution should complete once the finish signal fires");
        var lastProgress = progressUpdates.Last();
        Assert.True(lastProgress.CompletedSuccessfully, "Final progress should indicate successful completion");
        Assert.Contains("finish signal", lastProgress.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Writes a diagnostic line to the xUnit output sink, tolerating callbacks that
    ///     arrive after the test has finished. The adaptive-execution observable runs on a
    ///     background scheduler and can deliver a final emission just after the test method
    ///     returns; writing to <see cref="ITestOutputHelper" /> once the test is no longer
    ///     active throws <see cref="InvalidOperationException" />, which on a background
    ///     thread would crash the test host. Late writes are dropped silently.
    /// </summary>
    /// <param name="message">The diagnostic message to record.</param>
    private void SafeWriteLine(string message)
    {
        if (Volatile.Read(ref _testActive) == 0)
            return;

        try
        {
            _output.WriteLine(message);
        }
        catch (InvalidOperationException)
        {
            // The output sink is no longer bound to an active test; a late Rx callback
            // raced past test teardown. There is nothing useful to record at this point.
        }
    }

    /// <summary>
    ///     Marks the test as inactive so any Rx callbacks firing after teardown skip the
    ///     output sink instead of crashing the test host.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _testActive, 0);
    }
}