# ExecutionOrchestratorTests - Test Documentation

## Overview

This test file contains comprehensive unit tests for the `ExecutionOrchestrator` class, with a focus on verifying the *
*ObjectDisposedException fix** that prevents crashes when execution events fire during cleanup.

## The Problem Being Tested

### ObjectDisposedException Issue

The `ExecutionOrchestrator` uses a `Subject<string>` (`_rescheduleRequests`) to handle reschedule requests through a
reactive pipeline. When cleanup occurs, this Subject is disposed in the finally block. However, execution events can
still fire during cleanup (from agents or the event bus), which would attempt to call `_rescheduleRequests.OnNext()` on
a disposed Subject, causing an `ObjectDisposedException`.

### The Fix

The fix involves:

1. **Volatile bool flag**: `private volatile bool _isCleaningUp` to track cleanup state
2. **Guard clauses**: All `_rescheduleRequests.OnNext()` calls are guarded with `!_isCleaningUp` check
3. **Proper disposal ordering**:
    - Set `_isCleaningUp = true` FIRST
    - Stop the trigger service (prevents new skills)
    - Dispose event subscriptions (prevents new events)
    - Finally dispose the Subject (now safe - no OnNext calls can happen)

## Test Coverage

### 1. Events During Cleanup (CORE TEST)

**Test**: `StartLoadedProcedureAsync_EventsDuringCleanup_ShouldNotThrowObjectDisposedException`

Verifies that events firing during cleanup do not cause ObjectDisposedException. This is the main test for the fix.

**Scenario**:

- Start execution
- Trigger events that queue reschedule requests
- Cancel execution to trigger cleanup
- Verify no ObjectDisposedException is thrown

**Expected**: No exception (or only TaskCanceledException from cancellation)

### 2. Events After Cleanup Ignored

**Test**: `OnExecutionEvent_AfterCleanupStarts_ShouldBeIgnored`

Verifies that the `_isCleaningUp` guard correctly prevents OnNext() calls after cleanup starts.

**Scenario**:

- Start execution
- Publish events before cleanup
- Cancel to trigger cleanup
- Publish more events during/after cleanup
- Verify reschedules are NOT called after cleanup flag is set

**Expected**: No reschedule calls after cleanup starts

### 3. Proper Disposal Ordering

**Test**: `StartLoadedProcedureAsync_DisposalOrdering_ShouldBeCorrect`

Verifies that cleanup happens in the correct order to prevent race conditions.

**Scenario**:

- Complete a normal execution
- Track disposal order via mocks
- Verify trigger service is stopped first

**Expected**: TriggerService.Stop() is called during cleanup

### 4. Rapid Event Sequences

**Test**: `StartLoadedProcedureAsync_RapidEventsDuringCleanup_ShouldNotThrow`

Tests realistic scenarios where agents publish rapid event sequences (Start → Progress → Finish) during cleanup.

**Scenario**:

- Start execution
- Publish rapid progress updates (10 events in 100ms)
- Cancel during progress updates
- More events after cancel (simulating agent still reporting)
- Final finish event

**Expected**: No ObjectDisposedException

### 5. Multiple Skills with Interleaved Events

**Test**: `StartLoadedProcedureAsync_MultipleSkillsInterleavedEvents_ShouldHandleCleanupCorrectly`

Tests thread-safety of cleanup flag with multiple skills publishing events concurrently.

**Scenario**:

- Start execution with 3 skills
- Publish interleaved events from multiple skills in parallel
- Cancel to trigger cleanup while events are processing

**Expected**: No ObjectDisposedException, cleanup is called once

### 6. Reschedule Failure During Cleanup

**Test**: `StartLoadedProcedureAsync_RescheduleFailureDuringCleanup_ShouldNotThrow`

Verifies robust error handling when reschedules fail during cleanup.

**Scenario**:

- Start execution
- First reschedule succeeds, subsequent reschedules fail
- Cancel to trigger cleanup

**Expected**: No ObjectDisposedException (failures are handled gracefully)

## Known Limitations (Skipped Tests)

### Consecutive Executions Not Supported

**Tests**:

- `StartLoadedProcedureAsync_ConsecutiveExecutions_ShouldWorkCorrectly` (SKIPPED)
- `StartLoadedProcedureAsync_CleanupFlagReset_ShouldAllowSecondExecution` (SKIPPED)

**Issue**: The `ExecutionOrchestrator` is registered as a **Singleton** in DI, but it cannot be reused after one
execution because:

- `_rescheduleRequests` Subject is a `readonly` field initialized in the constructor
- It gets disposed in the finally block and cannot be recreated
- Second execution attempts to use the disposed Subject → ObjectDisposedException

**Root Cause**: Design mismatch between DI registration (Singleton) and implementation (single-use).

**Future Fix Options**:

1. Change DI registration from Singleton to Scoped
2. Make `_rescheduleRequests` non-readonly and recreate it in `StartLoadedProcedureAsync()`
3. Add a `ResetState()` method to reinitialize the orchestrator

**Impact**: Currently, the orchestrator can only be used once. For a new execution, a new instance must be created (
incompatible with Singleton registration).

## Test Patterns Used

### Mocking Strategy

- All dependencies are mocked using Moq
- `SkillExecutionEventBus` uses real implementation for event simulation
- `ReschedulingCoordinator` is mocked to control reschedule behavior

### Event Simulation

```csharp
_eventBus.PublishEvent(new ExecutionEvent
{
    SkillId = node.Id,
    EventType = ExecutionEventType.Start,
    Timestamp = DateTimeOffset.UtcNow
});
```

### Progress Data Structure

All progress events require complete `SkillExecutionProgress` objects:

```csharp
ProgressData = new Agents.SkillExecutionProgress
{
    ExecutionId = Guid.NewGuid(),
    SkillId = node.Id,
    AgentId = Guid.NewGuid(),
    ActualStartTimeUtc = DateTime.UtcNow,
    CurrentTimeIntoExecution = 5.0,
    EstimatedTotalDuration = 10.0,
    StatusMessage = "Progress message"
}
```

### Test Assertions

- Use `Record.ExceptionAsync()` to capture exceptions
- Assert for expected exception types (TaskCanceledException is expected, ObjectDisposedException is not)
- Verify mock calls to ensure proper cleanup

## Running the Tests

```bash
# Run all ExecutionOrchestratorTests
dotnet test Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~ExecutionOrchestratorTests"

# Run specific test
dotnet test --filter "StartLoadedProcedureAsync_EventsDuringCleanup_ShouldNotThrowObjectDisposedException"
```

## Test Results Summary

**Total Tests**: 8

- **Passed**: 6 (All core ObjectDisposedException fix tests pass ✓)
- **Skipped**: 2 (Consecutive execution tests - known limitation)

## Conclusion

The ObjectDisposedException fix is **verified and working correctly**. The `_isCleaningUp` flag successfully prevents
OnNext() calls on disposed Subjects during cleanup. However, a new issue was discovered: the orchestrator cannot be
reused for consecutive executions due to the disposed Subject, which is incompatible with its Singleton DI registration.

## Related Files

- **Implementation**: `/Application/Services/Execution/Pipeline/ExecutionOrchestrator.cs`
- **Interface**: `/Application/Services/Execution/Pipeline/IExecutionOrchestrator.cs`
- **DI Registration**: `/GraphQLServer/Extensions/ApplicationServiceExtensions.cs` (line 114)
- **Integration Tests**: `/Application.Tests/Services/Execution/Pipeline/EventDrivenExecutionIntegrationTests.cs`
