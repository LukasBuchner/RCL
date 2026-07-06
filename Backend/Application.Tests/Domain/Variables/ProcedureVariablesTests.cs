using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Tests.Domain.Variables;

/// <summary>
///     Tests for Procedure entity with Variables support.
/// </summary>
public class ProcedureVariablesTests
{
    [Fact]
    public void Procedure_Should_InitializeVariablesToEmptyList_When_NotProvided()
    {
        // Arrange & Act
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>()
        };

        // Assert
        Assert.NotNull(procedure.Variables);
        Assert.Empty(procedure.Variables);
    }

    [Fact]
    public void Procedure_Should_AllowMultipleVariables_When_Set()
    {
        // Arrange
        var variables = new List<VariableDefinition>
        {
            new()
            {
                Name = "var1",
                Type = new NumberType(),
                DefaultValue = 10
            },
            new()
            {
                Name = "var2",
                Type = new StringType(),
                DefaultValue = "test"
            },
            new()
            {
                Name = "var3",
                Type = new BooleanType(),
                DefaultValue = true
            }
        };

        // Act
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>(),
            Variables = variables
        };

        // Assert
        Assert.Equal(3, procedure.Variables.Count);
        Assert.Equal("var1", procedure.Variables[0].Name);
        Assert.IsType<NumberType>(procedure.Variables[0].Type);
        Assert.Equal(10, procedure.Variables[0].DefaultValue);
        Assert.Equal("var2", procedure.Variables[1].Name);
        Assert.Equal("var3", procedure.Variables[2].Name);
    }

    [Fact]
    public void RuntimeContext_Should_BeNullByDefault_When_NotPersisted()
    {
        // Arrange & Act
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>()
        };

        // Assert
        Assert.Null(procedure.RuntimeContext);
    }

    [Fact]
    public void RuntimeContext_Should_BeSettableAndRetrievable_When_Set()
    {
        // Arrange
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>()
        };

        var context = new VariableContext
        {
            ProcedureExecutionId = Guid.NewGuid()
        };

        // Act
        procedure.RuntimeContext = context;

        // Assert
        Assert.NotNull(procedure.RuntimeContext);
        Assert.Equal(context.ProcedureExecutionId, procedure.RuntimeContext.ProcedureExecutionId);
    }

    [Fact]
    public void RuntimeContext_Should_AllowSettingAndGettingValues_When_Attached()
    {
        // Arrange
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>(),
            Variables = new List<VariableDefinition>
            {
                new()
                {
                    Name = "counter",
                    Type = new NumberType(),
                    DefaultValue = 0
                }
            }
        };

        var context = new VariableContext();
        procedure.RuntimeContext = context;

        // Act
        procedure.RuntimeContext.SetValue("counter", 42);
        var value = procedure.RuntimeContext.GetValue<int>("counter");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void Variables_Should_SupportDifferentScopes_When_Defined()
    {
        // Arrange
        var variables = new List<VariableDefinition>
        {
            new()
            {
                Name = "procedureVar",
                Type = new StringType(),
                Scope = VariableScope.Procedure
            },
            new()
            {
                Name = "taskVar",
                Type = new NumberType(),
                Scope = VariableScope.Task
            },
            new()
            {
                Name = "globalVar",
                Type = new BooleanType(),
                Scope = VariableScope.Global
            }
        };

        // Act
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>(),
            Variables = variables
        };

        // Assert
        Assert.Equal(VariableScope.Procedure, procedure.Variables[0].Scope);
        Assert.Equal(VariableScope.Task, procedure.Variables[1].Scope);
        Assert.Equal(VariableScope.Global, procedure.Variables[2].Scope);
    }

    [Fact]
    public void Variables_Should_SupportDifferentSources_When_Defined()
    {
        // Arrange
        var variables = new List<VariableDefinition>
        {
            new()
            {
                Name = "userInput",
                Type = new StringType(),
                Source = VariableSource.UserDefined
            },
            new()
            {
                Name = "skillResult",
                Type = new NumberType(),
                Source = VariableSource.SkillOutput
            },
            new()
            {
                Name = "agentData",
                Type = new StringType(),
                Source = VariableSource.AgentState
            }
        };

        // Act
        var procedure = new Freydis.Domain.Entities.Procedure.Procedure
        {
            Name = "Test Procedure",
            RootNodeIds = new List<Guid>(),
            Variables = variables
        };

        // Assert
        Assert.Equal(VariableSource.UserDefined, procedure.Variables[0].Source);
        Assert.Equal(VariableSource.SkillOutput, procedure.Variables[1].Source);
        Assert.Equal(VariableSource.AgentState, procedure.Variables[2].Source);
    }
}