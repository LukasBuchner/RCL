using System.Reactive.Linq;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Tests.Services.Common.Reactive;

/// <summary>
///     Tests the variable face of <see cref="ProcedureStateTracker" /> (its
///     <see cref="IProcedureVariableChangeTracker" /> implementation), which tracks changes
///     to procedure variable definitions and exposes them as an observable stream.
/// </summary>
public sealed class ProcedureStateTrackerVariableTests : IDisposable
{
    private readonly ProcedureStateTracker _stateTracker;
    private readonly IProcedureVariableChangeTracker _tracker;

    public ProcedureStateTrackerVariableTests()
    {
        _stateTracker = new ProcedureStateTracker(
            new Mock<IProcedureRepository>().Object,
            new Mock<ILogger<ProcedureStateTracker>>().Object);
        _tracker = _stateTracker;
    }

    public void Dispose()
    {
        _stateTracker.Dispose();
    }

    private static IReadOnlyList<VariableDefinition> CreateTestVariables(params string[] names)
    {
        return names.Select(n => new VariableDefinition
        {
            Name = n,
            Type = new NumberType(),
            Source = VariableSource.UserDefined
        }).ToList().AsReadOnly();
    }

    [Fact]
    public async Task InitialValue_IsEmptyList()
    {
        // Act
        var initial = await _tracker.Variables.FirstAsync();

        // Assert
        initial.Should().BeEmpty("no procedure is loaded at startup");
    }

    [Fact]
    public async Task NotifyChanged_EmitsVariables()
    {
        // Arrange
        var variables = CreateTestVariables("speed", "position");
        var emitted = new List<IReadOnlyList<VariableDefinition>>();
        using var sub = _tracker.Variables.Skip(1).Subscribe(v => emitted.Add(v));

        // Act
        _tracker.NotifyChanged(variables);

        // Assert
        await Task.Delay(50);
        emitted.Should().ContainSingle();
        emitted[0].Should().HaveCount(2);
        emitted[0].Select(v => v.Name).Should().ContainInOrder("speed", "position");
    }

    [Fact]
    public async Task NotifyUnloaded_EmitsEmptyList()
    {
        // Arrange — first load some variables
        _tracker.NotifyChanged(CreateTestVariables("var1"));

        var emitted = new List<IReadOnlyList<VariableDefinition>>();
        using var sub = _tracker.Variables.Skip(1).Subscribe(v => emitted.Add(v));

        // Act
        _tracker.NotifyUnloaded();

        // Assert
        await Task.Delay(50);
        emitted.Should().ContainSingle()
            .Which.Should().BeEmpty("unloading clears the variable list");
    }

    [Fact]
    public async Task MultipleNotifications_EmitsEachInOrder()
    {
        // Arrange
        var vars1 = CreateTestVariables("alpha");
        var vars2 = CreateTestVariables("beta", "gamma");
        var emitted = new List<IReadOnlyList<VariableDefinition>>();
        using var sub = _tracker.Variables.Skip(1).Subscribe(v => emitted.Add(v));

        // Act
        _tracker.NotifyChanged(vars1);
        _tracker.NotifyChanged(vars2);
        _tracker.NotifyUnloaded();

        // Assert
        await Task.Delay(50);
        emitted.Should().HaveCount(3);
        emitted[0].Should().ContainSingle(v => v.Name == "alpha");
        emitted[1].Should().HaveCount(2);
        emitted[2].Should().BeEmpty();
    }

    [Fact]
    public async Task NewSubscriber_ReceivesCurrentValue()
    {
        // Arrange — notify before subscribing
        var variables = CreateTestVariables("already_set");
        _tracker.NotifyChanged(variables);

        // Act — subscribe after the notification
        var current = await _tracker.Variables.FirstAsync();

        // Assert
        current.Should().ContainSingle()
            .Which.Name.Should().Be("already_set",
                "BehaviorSubject replays the latest value to new subscribers");
    }

    [Fact]
    public async Task NotifyChanged_WithUpdatedVariables_EmitsUpdatedList()
    {
        // Arrange — simulate initial variables
        var initial = CreateTestVariables("output_position");
        _tracker.NotifyChanged(initial);

        // Now add a second variable
        var updated = CreateTestVariables("output_position", "output_force");

        var emitted = new List<IReadOnlyList<VariableDefinition>>();
        using var sub = _tracker.Variables.Skip(1).Subscribe(v => emitted.Add(v));

        // Act
        _tracker.NotifyChanged(updated);

        // Assert
        await Task.Delay(50);
        emitted.Should().ContainSingle();
        emitted[0].Should().HaveCount(2);
        emitted[0].Select(v => v.Name).Should().Contain("output_force");
    }
}