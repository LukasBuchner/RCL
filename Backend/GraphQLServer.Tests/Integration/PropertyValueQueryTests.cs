using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Operations;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types.OutputTypes;
using FHOOE.Freydis.GraphQLServer.Types.Resolvers;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FHOOE.Freydis.GraphQLServer.Tests.Integration;

/// <summary>
///     Integration tests for GraphQL TypedProperty.value field resolution.
///     Tests that TypedValue is correctly mapped to PropertyValue union types.
/// </summary>
public class PropertyValueQueryTests : IAsyncLifetime
{
    private IRequestExecutor _executor = null!;

    public async Task InitializeAsync()
    {
        // Setup test GraphQL executor with mock data
        var services = new ServiceCollection();

        // Create test skill with different property types
        var testSkillId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var testSkill = new Skill
        {
            Id = testSkillId,
            Name = "TestSkill",
            Description = "Test skill for property value tests",
            Properties =
            [
                new TypedProperty
                {
                    Name = "BoolProp",
                    Direction = PropertyDirection.Input,
                    Value = TypedValue.Boolean(true)
                },

                new TypedProperty
                {
                    Name = "NumberProp",
                    Direction = PropertyDirection.Input,
                    Value = TypedValue.Number(42.5)
                },

                new TypedProperty
                {
                    Name = "PositionProp",
                    Direction = PropertyDirection.Input,
                    Value = TypedValue.Position(new Position { X = 1.0, Y = 2.0, Z = 3.0 })
                }
            ]
        };

        // Mock skill service
        var mockSkillService = new Mock<ISkillApplicationService>();
        mockSkillService.Setup(s => s.GetSkillByIdAsync(testSkillId))
            .ReturnsAsync(testSkill);

        // Register required services
        services.AddLogging();
        services.AddSingleton<ITypedValueMapper, TypedValueMapper>();
        services.AddSingleton(mockSkillService.Object);

        // Use schema-first approach with a minimal test schema
        // This avoids ValueType input type discovery issues that occur with code-first
        var schemaPath = "./Integration/test-schema.graphql";

        services
            .AddGraphQLServer()
            .AddDocumentFromFile(schemaPath)
            .BindRuntimeType<Query>()
            .BindRuntimeType<Skill>()
            .BindRuntimeType<TypedProperty>()
            .BindRuntimeType<Position>()
            .BindRuntimeType<BooleanValue>()
            .BindRuntimeType<NumberValue>()
            .BindRuntimeType<StringValue>()
            .BindRuntimeType<PositionValue>()
            .AddResolver<PropertyResolvers>("Property")
            .AddInMemorySubscriptions();

        var provider = services.BuildServiceProvider();
        _executor = await provider.GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Query_Skill_WithBooleanProperty_ReturnsCorrectValue()
    {
        // Arrange
        var query = @"
            query {
                skillById(id: ""00000000-0000-0000-0000-000000000001"") {
                    properties {
                        name
                        value {
                            __typename
                            ... on BooleanValue {
                                boolValue
                            }
                        }
                    }
                }
            }
        ";

        // Act
        var result = await _executor.ExecuteAsync(query);

        // Assert
        var data = result.ToJson();
        Assert.Contains("\"boolValue\": true", data);
        Assert.Contains("\"__typename\": \"BooleanValue\"", data);
    }

    [Fact]
    public async Task Query_Skill_WithNumberProperty_ReturnsCorrectValue()
    {
        // Arrange
        var query = @"
            query {
                skillById(id: ""00000000-0000-0000-0000-000000000001"") {
                    properties {
                        name
                        value {
                            __typename
                            ... on NumberValue {
                                numberValue
                            }
                        }
                    }
                }
            }
        ";

        // Act
        var result = await _executor.ExecuteAsync(query);

        // Assert
        var data = result.ToJson();
        Assert.Contains("\"numberValue\": 42.5", data);
        Assert.Contains("\"__typename\": \"NumberValue\"", data);
    }

    [Fact]
    public async Task Query_Skill_WithPositionProperty_ReturnsCorrectValue()
    {
        // Arrange
        var query = @"
            query {
                skillById(id: ""00000000-0000-0000-0000-000000000001"") {
                    properties {
                        name
                        value {
                            __typename
                            ... on PositionValue {
                                positionValue {
                                    x
                                    y
                                    z
                                }
                            }
                        }
                    }
                }
            }
        ";

        // Act
        var result = await _executor.ExecuteAsync(query);

        // Assert
        var data = result.ToJson();
        Assert.Contains("\"x\": 1", data); // Accept both 1.0 and 1 as valid JSON representations
        Assert.Contains("\"__typename\": \"PositionValue\"", data);
    }
}