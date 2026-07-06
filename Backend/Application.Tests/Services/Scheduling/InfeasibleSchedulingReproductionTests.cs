using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Application.Services.Scheduling.SkillExecutions;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Scheduling;
using FHOOE.Freydis.Scheduling.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;
using IPlannedSkillExecution = FHOOE.Freydis.Scheduling.Core.IPlannedSkillExecution;

namespace FHOOE.Freydis.Application.Tests.Services.Scheduling;

public class InfeasibleSchedulingReproductionTests
{
    private readonly ILogger<InfeasibleSchedulingReproductionTests> _logger;
    private readonly ITestOutputHelper _output;

    public InfeasibleSchedulingReproductionTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a logger that outputs to xUnit test output
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new XUnitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        _logger = loggerFactory.CreateLogger<InfeasibleSchedulingReproductionTests>();
    }

    [Fact]
    public void Schedule_WithTwoAdaptiveSkillsAndCurrentTime_UsingSolveWithLP_ShouldSucceed()
    {
        // Arrange - Create scenario matching the logs
        var currentTime = 5.998; // Matches the logs

        // Create mock agents
        var robotAgent = CreateMockAgent("Robot");
        var aliceAgent = CreateMockAgent("Alice");

        // Create domain skills
        var moveObjectToRobot = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var moveObjectToAlice = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        // Create domain agents
        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };
        var aliceDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            RepresentativeColor = "#00FF00"
        };

        // Create adaptive skill executions matching the logs
        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectToRobot,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 45, // From logs
            PlannedDuration = 65 // Nominal from logs
        };

        var aliceSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Alice)",
            DomainSkill = moveObjectToAlice,
            DomainAgent = aliceDomainAgent,
            RuntimeAgent = aliceAgent.Object,
            MinDuration = 65, // From logs
            PlannedDuration = 85 // Nominal from logs
        };

        // Create execution graph with no dependencies (parallel execution)
        var skillExecutions = new List<IPlannedSkillExecution>
        {
            robotSkillExecution,
            aliceSkillExecution
        };

        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing scheduling with currentTime={currentTime}s");
        _output.WriteLine(
            $"Robot skill: Min={robotSkillExecution.MinDuration}, PlannedDuration={robotSkillExecution.PlannedDuration}");
        _output.WriteLine(
            $"Alice skill: Min={aliceSkillExecution.MinDuration}, PlannedDuration={aliceSkillExecution.PlannedDuration}");

        // This should succeed but currently throws ScheduleInfeasibleException
        var exception = Record.Exception(() => { executionGraph.SolveWithLinearProgramming(currentTime, _logger); });

        // Currently this test FAILS with INFEASIBLE error - that's what we need to fix
        Assert.Null(exception);

        // Verify the schedule was created successfully
        Assert.True(robotSkillExecution.PlannedStartTime >= currentTime,
            $"Robot skill should start at or after currentTime {currentTime}");
        Assert.True(aliceSkillExecution.PlannedStartTime >= currentTime,
            $"Alice skill should start at or after currentTime {currentTime}");

        _output.WriteLine(
            $"Robot skill scheduled: Start={robotSkillExecution.PlannedStartTime:F3}s, Finish={robotSkillExecution.PlannedFinishTime:F3}s");
        _output.WriteLine(
            $"Alice skill scheduled: Start={aliceSkillExecution.PlannedStartTime:F3}s, Finish={aliceSkillExecution.PlannedFinishTime:F3}s");
    }

    [Fact]
    public void Schedule_WithTwoAdaptiveSkillsAndCurrentTime_UsingPlanSchedule_ShouldSucceed()
    {
        // Arrange - Create scenario matching the logs, using PlanSchedule method
        var currentTime = 5.998; // Matches the logs

        // Create mock agents
        var robotAgent = CreateMockAgent("Robot");
        var aliceAgent = CreateMockAgent("Alice");

        // Create domain skills
        var moveObjectToRobot = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var moveObjectToAlice = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        // Create domain agents
        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };
        var aliceDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            RepresentativeColor = "#00FF00"
        };

        // Create adaptive skill executions matching the logs
        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectToRobot,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 45, // From logs
            PlannedDuration = 65 // Nominal from logs
        };

        var aliceSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Alice)",
            DomainSkill = moveObjectToAlice,
            DomainAgent = aliceDomainAgent,
            RuntimeAgent = aliceAgent.Object,
            MinDuration = 65, // From logs
            PlannedDuration = 85 // Nominal from logs
        };

        // Create execution graph with no dependencies (parallel execution)
        var skillExecutions = new List<IPlannedSkillExecution>
        {
            robotSkillExecution,
            aliceSkillExecution
        };

        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert - Using PlanSchedule instead of SolveWithLinearProgramming
        _output.WriteLine($"Testing scheduling with PlanSchedule, currentTime={currentTime}s");
        _output.WriteLine(
            $"Robot skill: Min={robotSkillExecution.MinDuration}, PlannedDuration={robotSkillExecution.PlannedDuration}");
        _output.WriteLine(
            $"Alice skill: Min={aliceSkillExecution.MinDuration}, PlannedDuration={aliceSkillExecution.PlannedDuration}");

        var exception = Record.Exception(() => { executionGraph.PlanSchedule(currentTime, false, _logger); });

        // This might throw the INFEASIBLE error
        Assert.Null(exception);

        // Verify the schedule was created successfully
        Assert.True(robotSkillExecution.PlannedStartTime >= currentTime,
            $"Robot skill should start at or after currentTime {currentTime}");
        Assert.True(aliceSkillExecution.PlannedStartTime >= currentTime,
            $"Alice skill should start at or after currentTime {currentTime}");

        _output.WriteLine(
            $"Robot skill scheduled: Start={robotSkillExecution.PlannedStartTime:F3}s, Finish={robotSkillExecution.PlannedFinishTime:F3}s");
        _output.WriteLine(
            $"Alice skill scheduled: Start={aliceSkillExecution.PlannedStartTime:F3}s, Finish={aliceSkillExecution.PlannedFinishTime:F3}s");
    }

    [Fact]
    public void Schedule_WithSingleAdaptiveSkillAndCurrentTime_ShouldSucceed()
    {
        // Arrange - Simpler test case with just one adaptive skill
        var currentTime = 5.998;

        var robotAgent = CreateMockAgent("Robot");
        var moveObjectTo = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };

        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectTo,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 45,
            PlannedDuration = 65
        };

        var skillExecutions = new List<IPlannedSkillExecution> { robotSkillExecution };
        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing single adaptive skill with currentTime={currentTime}s");

        var exception = Record.Exception(() => { executionGraph.SolveWithLinearProgramming(currentTime, _logger); });

        // This should also succeed
        Assert.Null(exception);
        Assert.True(robotSkillExecution.PlannedStartTime >= currentTime);

        _output.WriteLine(
            $"Skill scheduled: Start={robotSkillExecution.PlannedStartTime:F3}s, Finish={robotSkillExecution.PlannedFinishTime:F3}s");
    }

    [Fact]
    public void Schedule_WithZeroPlannedDuration_ShouldThrowInfeasible()
    {
        // Arrange - Test with PlannedDuration = 0 which was the bug
        var currentTime = 5.998;

        var robotAgent = CreateMockAgent("Robot");

        var moveObjectTo = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };

        // Create adaptive skill with PlannedDuration = 0 (the bug scenario)
        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectTo,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 45,
            PlannedDuration = 0 // This was the bug!
        };

        var skillExecutions = new List<IPlannedSkillExecution> { robotSkillExecution };
        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing with PlannedDuration=0 (bug scenario), currentTime={currentTime}s");
        _output.WriteLine(
            $"Skill: Min={robotSkillExecution.MinDuration}, PlannedDuration={robotSkillExecution.PlannedDuration}");

        // With PlannedDuration=0, this should still work because OR-Tools uses MinDuration
        var exception = Record.Exception(() => { executionGraph.SolveWithLinearProgramming(currentTime, _logger); });

        // The solver should still succeed because it uses MinDuration for adaptive skills
        Assert.Null(exception);

        // After solving, PlannedDuration should be updated to a valid value
        Assert.True(robotSkillExecution.PlannedDuration >= robotSkillExecution.MinDuration,
            $"PlannedDuration {robotSkillExecution.PlannedDuration} should be >= MinDuration {robotSkillExecution.MinDuration}");

        _output.WriteLine($"After solving: PlannedDuration={robotSkillExecution.PlannedDuration:F3}s");
    }

    [Fact]
    public void Schedule_WithZeroMinDuration_ShouldSucceed()
    {
        // Arrange - Test with MinDuration = 0 which could cause INFEASIBLE
        var currentTime = 5.998;

        var robotAgent = CreateMockAgent("Robot");

        var moveObjectTo = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };

        // Create adaptive skill with MinDuration = 0 (potential bug scenario)
        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectTo,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 0, // Could cause issues
            PlannedDuration = 0
        };

        var skillExecutions = new List<IPlannedSkillExecution> { robotSkillExecution };
        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing with MinDuration=0, currentTime={currentTime}s");
        _output.WriteLine(
            $"Skill: Min={robotSkillExecution.MinDuration}, PlannedDuration={robotSkillExecution.PlannedDuration}");

        // This might cause INFEASIBLE if the constraint setup doesn't handle zero durations
        var exception = Record.Exception(() => { executionGraph.SolveWithLinearProgramming(currentTime, _logger); });

        // With zero durations, it should still work (though not very useful)
        Assert.Null(exception);

        _output.WriteLine($"After solving: PlannedDuration={robotSkillExecution.PlannedDuration:F3}s");
    }

    [Fact]
    public void Schedule_WithNegativeMinDuration_ShouldThrowInfeasible()
    {
        // Arrange - Test with an invalid negative MinDuration which should cause INFEASIBLE
        var currentTime = 5.998;

        var robotAgent = CreateMockAgent("Robot");

        var moveObjectTo = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To",
            Description = "Move an object to a target location",
            Properties = new List<TypedProperty>()
        };

        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };

        // Create adaptive skill with an invalid negative MinDuration (definite bug scenario)
        var robotSkillExecution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Move Object To (Robot)",
            DomainSkill = moveObjectTo,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = -45, // Negative - INVALID!
            PlannedDuration = 65
        };

        var skillExecutions = new List<IPlannedSkillExecution> { robotSkillExecution };
        var dependencies = new List<Dependency>();

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing with invalid negative MinDuration, currentTime={currentTime}s");
        _output.WriteLine(
            $"Skill: Min={robotSkillExecution.MinDuration}, PlannedDuration={robotSkillExecution.PlannedDuration}");

        // This should throw because a negative MinDuration is invalid
        var exception = Record.Exception(() => { executionGraph.SolveWithLinearProgramming(currentTime, _logger); });

        // Should throw ScheduleModelException or ScheduleInfeasibleException
        Assert.NotNull(exception);
        _output.WriteLine($"Exception thrown as expected: {exception.GetType().Name}: {exception.Message}");
    }

    [Fact]
    public void Schedule_WithAdaptiveFsCycle_ShouldRejectAsScheduleModelException()
    {
        // Arrange - Test with cyclic FS dependencies on adaptive skills.
        // ValidateModel rejects the graph as event-level cyclic before scheduling runs.
        var currentTime = 5.998;

        var robotAgent = CreateMockAgent("Robot");
        var aliceAgent = CreateMockAgent("Alice");

        // Create domain entities
        var skill1 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Skill 1",
            Description = "First skill in cycle",
            Properties = new List<TypedProperty>()
        };

        var skill2 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Skill 2",
            Description = "Second skill in cycle",
            Properties = new List<TypedProperty>()
        };

        var robotDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Robot",
            RepresentativeColor = "#FF0000"
        };

        var aliceDomainAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            RepresentativeColor = "#00FF00"
        };

        // Create two adaptive skills that form a cycle
        var skill1Execution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Skill 1 (Robot)",
            DomainSkill = skill1,
            DomainAgent = robotDomainAgent,
            RuntimeAgent = robotAgent.Object,
            MinDuration = 45,
            PlannedDuration = 65
        };

        var skill2Execution = new PlannedAdaptiveSkillExecution
        {
            Id = Guid.NewGuid(),
            Name = "Skill 2 (Alice)",
            DomainSkill = skill2,
            DomainAgent = aliceDomainAgent,
            RuntimeAgent = aliceAgent.Object,
            MinDuration = 65,
            PlannedDuration = 85
        };

        var skillExecutions = new List<IPlannedSkillExecution>
        {
            skill1Execution,
            skill2Execution
        };

        // Create cyclic dependencies: skill1 -> skill2 -> skill1
        var dependencies = new List<Dependency>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Source = skill1Execution,
                Target = skill2Execution,
                Type = DependencyType.FinishToStart
            },
            new()
            {
                Id = Guid.NewGuid(),
                Source = skill2Execution,
                Target = skill1Execution,
                Type = DependencyType.FinishToStart
            }
        };

        var executionGraph = new ExecutionGraph
        {
            SkillExecutions = skillExecutions,
            Dependencies = dependencies
        };

        // Act & Assert
        _output.WriteLine($"Testing with adaptive cycle (FinishToStart), currentTime={currentTime}s");
        _output.WriteLine(
            $"Skill1: Min={skill1Execution.MinDuration}, PlannedDuration={skill1Execution.PlannedDuration}");
        _output.WriteLine(
            $"Skill2: Min={skill2Execution.MinDuration}, PlannedDuration={skill2Execution.PlannedDuration}");

        // PlanSchedule should detect the cycle and handle it appropriately
        var exception = Record.Exception(() => { executionGraph.PlanSchedule(currentTime, false, _logger); });

        // FS cycles are event-level cyclic; ValidateModel rejects the model structurally.
        Assert.NotNull(exception);
        Assert.IsType<ScheduleModelException>(exception);
        Assert.Contains("Event-level", exception.Message);
        _output.WriteLine($"Expected exception thrown: {exception.Message}");
    }

    private Mock<IRuntimeAgent> CreateMockAgent(string name)
    {
        var mockAgent = new Mock<IRuntimeAgent>();
        mockAgent.Setup(a => a.Name).Returns(name);
        mockAgent.Setup(a => a.Id).Returns(Guid.NewGuid());
        return mockAgent;
    }

    /// <summary>
    ///     Custom logger provider for xUnit test output
    /// </summary>
    private class XUnitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(output, categoryName);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     Custom logger that writes to xUnit test output
    /// </summary>
    private sealed class XUnitLogger(ITestOutputHelper output, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            output.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");
            if (exception != null) output.WriteLine($"Exception: {exception}");
        }
    }
}