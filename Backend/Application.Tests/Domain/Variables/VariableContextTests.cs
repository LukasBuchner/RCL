using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Tests.Domain.Variables;

/// <summary>
///     Tests for VariableContext class including thread-safety.
/// </summary>
public class VariableContextTests
{
    [Fact]
    public void GetValue_Should_ReturnCorrectTypedValue_When_VariableExists()
    {
        // Arrange
        var context = new VariableContext();
        context.SetValue("counter", 42);

        // Act
        var value = context.GetValue<int>("counter");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValue_Should_ReturnDefault_When_VariableDoesNotExist()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        var intValue = context.GetValue<int>("nonexistent");
        var stringValue = context.GetValue<string>("nonexistent");

        // Assert
        Assert.Equal(0, intValue);
        Assert.Null(stringValue);
    }

    [Fact]
    public void SetValue_Should_StoreValueCorrectly_When_Called()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.SetValue("testVar", "testValue");
        var value = context.GetValue<string>("testVar");

        // Assert
        Assert.Equal("testValue", value);
    }

    [Fact]
    public void SetValue_Should_UpdateLastUpdatedUtc_When_Called()
    {
        // Arrange
        var context = new VariableContext();
        var beforeSet = DateTime.UtcNow;

        // Act
        context.SetValue("testVar", 123);
        var afterSet = DateTime.UtcNow;

        // Assert
        Assert.True(context.LastUpdatedUtc >= beforeSet);
        Assert.True(context.LastUpdatedUtc <= afterSet);
    }

    [Fact]
    public void SetValue_Should_StoreUpdatedBy_When_Provided()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        context.SetValue("testVar", 100, "SkillA");
        var allValues = context.GetAllValues();

        // Assert
        Assert.True(allValues.ContainsKey("testVar"));
        Assert.Equal("SkillA", allValues["testVar"].LastUpdatedBy);
    }

    [Fact]
    public void TryGetValue_Should_ReturnTrue_When_VariableExists()
    {
        // Arrange
        var context = new VariableContext();
        context.SetValue("exists", "yes");

        // Act
        var result = context.TryGetValue("exists", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("yes", value);
    }

    [Fact]
    public void TryGetValue_Should_ReturnFalse_When_VariableDoesNotExist()
    {
        // Arrange
        var context = new VariableContext();

        // Act
        var result = context.TryGetValue("nonexistent", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void GetAllValues_Should_ReturnAllStoredValues_When_Called()
    {
        // Arrange
        var context = new VariableContext();
        context.SetValue("var1", 10);
        context.SetValue("var2", "test");
        context.SetValue("var3", true);

        // Act
        var allValues = context.GetAllValues();

        // Assert
        Assert.Equal(3, allValues.Count);
        Assert.True(allValues.ContainsKey("var1"));
        Assert.True(allValues.ContainsKey("var2"));
        Assert.True(allValues.ContainsKey("var3"));
        Assert.Equal(10, allValues["var1"].Value);
        Assert.Equal("test", allValues["var2"].Value);
        Assert.Equal(true, allValues["var3"].Value);
    }

    [Fact]
    public void ConcurrentSetValue_Should_NotLoseData_When_MultipleThreadsWrite()
    {
        // Arrange
        var context = new VariableContext();
        var threadCount = 10;
        var iterationsPerThread = 100;
        var threads = new List<Thread>();

        // Act
        for (var i = 0; i < threadCount; i++)
        {
            var threadId = i;
            var thread = new Thread(() =>
            {
                for (var j = 0; j < iterationsPerThread; j++)
                    context.SetValue($"var_{threadId}_{j}", threadId * 1000 + j);
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads) thread.Join();

        // Assert
        var allValues = context.GetAllValues();
        Assert.Equal(threadCount * iterationsPerThread, allValues.Count);

        // Verify all expected values exist
        for (var i = 0; i < threadCount; i++)
            for (var j = 0; j < iterationsPerThread; j++)
            {
                var key = $"var_{i}_{j}";
                Assert.True(allValues.ContainsKey(key), $"Missing key: {key}");
                Assert.Equal(i * 1000 + j, allValues[key].Value);
            }
    }

    [Fact]
    public void ConcurrentGetSetValue_Should_WorkCorrectly_When_MixedOperations()
    {
        // Arrange
        var context = new VariableContext();
        context.SetValue("shared", 0);
        var readThreads = new List<Thread>();
        var writeThreads = new List<Thread>();
        var readCount = 5;
        var writeCount = 5;
        var iterations = 50;

        // Act
        // Start read threads
        for (var i = 0; i < readCount; i++)
        {
            var thread = new Thread(() =>
            {
                for (var j = 0; j < iterations; j++)
                {
                    var value = context.GetValue<int>("shared");
                    // Just verify we can read without exceptions
                    Assert.True(value >= 0);
                }
            });
            readThreads.Add(thread);
            thread.Start();
        }

        // Start write threads
        for (var i = 0; i < writeCount; i++)
        {
            var threadId = i;
            var thread = new Thread(() =>
            {
                for (var j = 0; j < iterations; j++) context.SetValue("shared", threadId * 100 + j);
            });
            writeThreads.Add(thread);
            thread.Start();
        }

        // Wait for all threads
        foreach (var thread in readThreads.Concat(writeThreads)) thread.Join();

        // Assert - No exceptions and context is still valid
        var finalValue = context.GetValue<int>("shared");
        Assert.True(finalValue >= 0);
    }

    [Fact]
    public void GenericTypeConversion_Should_Work_When_TypesAreCompatible()
    {
        // Arrange
        var context = new VariableContext();
        context.SetValue("number", 42);

        // Act
        var asInt = context.GetValue<int>("number");
        var asObject = context.GetValue<object>("number");

        // Assert
        Assert.Equal(42, asInt);
        Assert.Equal(42, asObject);
    }

    [Fact]
    public void ProcedureExecutionId_Should_BeSettable_When_PropertyExists()
    {
        // Arrange
        var context = new VariableContext();
        var executionId = Guid.NewGuid();

        // Act
        context.ProcedureExecutionId = executionId;

        // Assert
        Assert.Equal(executionId, context.ProcedureExecutionId);
    }

    [Fact]
    public void Changes_Observable_Should_EmitNotification_When_SetValueCalled()
    {
        // Arrange
        var context = new VariableContext();
        VariableValue? receivedValue = null;

        // Subscribe to changes
        context.Changes.Subscribe(value => receivedValue = value);

        // Act
        context.SetValue("testVar", 123, "TestSource");

        // Assert
        Assert.NotNull(receivedValue);
        Assert.Equal("testVar", receivedValue.Name);
        Assert.Equal(123, receivedValue.Value);
        Assert.Equal("TestSource", receivedValue.LastUpdatedBy);
    }

    [Fact]
    public void Changes_Observable_Should_EmitMultipleNotifications_When_MultipleSetValueCalls()
    {
        // Arrange
        var context = new VariableContext();
        var receivedValues = new List<VariableValue>();

        context.Changes.Subscribe(value => receivedValues.Add(value));

        // Act
        context.SetValue("var1", 10);
        context.SetValue("var2", "test");
        context.SetValue("var3", true);

        // Assert
        Assert.Equal(3, receivedValues.Count);
        Assert.Equal("var1", receivedValues[0].Name);
        Assert.Equal(10, receivedValues[0].Value);
        Assert.Equal("var2", receivedValues[1].Name);
        Assert.Equal("test", receivedValues[1].Value);
        Assert.Equal("var3", receivedValues[2].Name);
        Assert.Equal(true, receivedValues[2].Value);
    }

    [Fact]
    public void Changes_Observable_Should_NotifyMultipleSubscribers_When_SetValueCalled()
    {
        // Arrange
        var context = new VariableContext();
        VariableValue? subscriber1Value = null;
        VariableValue? subscriber2Value = null;

        context.Changes.Subscribe(value => subscriber1Value = value);
        context.Changes.Subscribe(value => subscriber2Value = value);

        // Act
        context.SetValue("testVar", 42);

        // Assert
        Assert.NotNull(subscriber1Value);
        Assert.NotNull(subscriber2Value);
        Assert.Equal("testVar", subscriber1Value.Name);
        Assert.Equal("testVar", subscriber2Value.Name);
        Assert.Equal(42, subscriber1Value.Value);
        Assert.Equal(42, subscriber2Value.Value);
    }

    [Fact]
    public void Changes_Observable_Should_StopNotifying_When_Unsubscribed()
    {
        // Arrange
        var context = new VariableContext();
        var receivedCount = 0;

        var subscription = context.Changes.Subscribe(_ => receivedCount++);

        // Act
        context.SetValue("var1", 1);
        subscription.Dispose(); // Unsubscribe
        context.SetValue("var2", 2);

        // Assert
        Assert.Equal(1, receivedCount); // Only the first value before unsubscribe
    }
}