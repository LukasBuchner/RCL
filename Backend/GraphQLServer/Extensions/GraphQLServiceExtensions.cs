using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.GraphQLServer.Operations;
using FHOOE.Freydis.GraphQLServer.Services;
using FHOOE.Freydis.GraphQLServer.Services.DataLoaders;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Types;
using FHOOE.Freydis.GraphQLServer.Types.DTOs;
using FHOOE.Freydis.GraphQLServer.Types.InputTypes;
using FHOOE.Freydis.GraphQLServer.Types.OutputTypes;
using FHOOE.Freydis.GraphQLServer.Types.Resolvers;
using BooleanType = FHOOE.Freydis.Domain.Entities.Common.BooleanType;
using EnumType = FHOOE.Freydis.Domain.Entities.Common.EnumType;
using ListType = FHOOE.Freydis.Domain.Entities.Common.ListType;
using StringType = FHOOE.Freydis.Domain.Entities.Common.StringType;
using ValueType = FHOOE.Freydis.Domain.Entities.Common.ValueType;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring GraphQL services.
/// </summary>
public static class GraphQlServiceExtensions
{
    /// <summary>
    ///     Adds and configures GraphQL services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGraphQlServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register GraphQL-specific services
        services.AddScoped<RuntimeAgentService>();
        services.AddScoped<ITypedValueMapper, TypedValueMapper>();
        services.AddSingleton<GraphQlErrorFilter>(); // Must be Singleton for GraphQL schema validation
        services.AddSingleton<GraphQlDiagnosticEventListener>();

        // Register DataLoaders following official HotChocolate v15 pattern
        services.AddScoped<AgentDataLoader>();
        services.AddScoped<SkillDataLoader>();
        services.AddScoped<AgentsBySkillIdDataLoader>();

        // Determine if introspection should be disabled based on environment
        var environment = services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();

        // Configure GraphQL server with schema-first approach
        services
            .AddGraphQLServer()
            .DisableIntrospection(environment.IsProduction()) // Disable introspection only in Production
            .AddDocumentFromFile("./schema.graphql")
            .BindRuntimeType<Query>()
            .BindRuntimeType<Mutation>()
            .BindRuntimeType<Subscription>()
            .BindRuntimeType<Agent>()
            .BindRuntimeType<Skill>()
            .BindRuntimeType<SceneObject>()
            .BindRuntimeType<PositionTag>()
            .BindRuntimeType<DependencyEdge>()
            .BindRuntimeType<Node>()
            .BindRuntimeType<SkillExecutionNode>()
            .BindRuntimeType<TaskNode>()
            .BindRuntimeType<SkillExecutionTask>()
            .BindRuntimeType<TaskDto>()
            .BindRuntimeType<RouterNode>()
            .BindRuntimeType<RouterTask>()
            .BindRuntimeType<ConditionalBranch>()
            .BindRuntimeType<SelectorExpression>()
            .BindRuntimeType<SimpleVariableSelector>()
            .BindRuntimeType<ExpressionSelector>()
            .AddResolver<SkillExecutionTaskResolvers>("SkillExecutionTask")
            .AddResolver<SkillResolvers>("Skill")
            .AddResolver<AgentResolvers>("Agent")
            .AddResolver<PropertyResolvers>("Property")
            .BindRuntimeType<ExecutionTimingDto>()
            .BindRuntimeType<RuntimeAgentInfo>()
            .BindRuntimeType<Position>()
            .BindRuntimeType<TypedProperty>()
            .BindRuntimeType<ValueType>()
            .BindRuntimeType<BooleanType>()
            .BindRuntimeType<NumberType>()
            .BindRuntimeType<StringType>()
            .BindRuntimeType<PositionType>()
            .BindRuntimeType<PositionTagType>()
            .BindRuntimeType<SceneObjectType>()
            .BindRuntimeType<EnumType>()
            .BindRuntimeType<ListType>()
            .BindRuntimeType<BooleanValue>()
            .BindRuntimeType<NumberValue>()
            .BindRuntimeType<StringValue>()
            .BindRuntimeType<PositionValue>()
            .BindRuntimeType<PositionTagValue>()
            .BindRuntimeType<SceneObjectValue>()
            .BindRuntimeType<CreateAgentPayload>()
            .BindRuntimeType<CreateDependencyEdgePayload>()
            .BindRuntimeType<CreateNodePayload>()
            .BindRuntimeType<CreatePositionTagPayload>()
            .BindRuntimeType<CreateSceneObjectPayload>()
            .BindRuntimeType<CreateSkillPayload>()
            .BindRuntimeType<DeleteAgentPayload>()
            .BindRuntimeType<DeleteDependencyEdgePayload>()
            .BindRuntimeType<DeleteNodePayload>()
            .BindRuntimeType<DeletePositionTagPayload>()
            .BindRuntimeType<DeleteSceneObjectPayload>()
            .BindRuntimeType<DeleteSkillPayload>()
            .BindRuntimeType<StartLoadedProcedurePayload>()
            .BindRuntimeType<UpdateAgentPayload>()
            .BindRuntimeType<UpdateDependencyEdgePayload>()
            .BindRuntimeType<UpdateNodePayload>()
            .BindRuntimeType<UpdatePositionTagPayload>()
            .BindRuntimeType<UpdateSceneObjectPayload>()
            .BindRuntimeType<UpdateSkillPayload>()
            .BindRuntimeType<CreateAgentInput>()
            .BindRuntimeType<CreateDependencyEdgeInput>()
            .BindRuntimeType<CreateNodeInput>()
            .BindRuntimeType<CreatePositionTagInput>()
            .BindRuntimeType<CreateSceneObjectInput>()
            .BindRuntimeType<CreateSkillInput>()
            .BindRuntimeType<DeleteAgentInput>()
            .BindRuntimeType<DeleteDependencyEdgeInput>()
            .BindRuntimeType<DeleteNodeInput>()
            .BindRuntimeType<DeletePositionTagInput>()
            .BindRuntimeType<DeleteSceneObjectInput>()
            .BindRuntimeType<DeleteSkillInput>()
            .BindRuntimeType<UpdateAgentInput>()
            .BindRuntimeType<UpdateDependencyEdgeInput>()
            .BindRuntimeType<UpdateNodeInput>()
            .BindRuntimeType<UpdatePositionTagInput>()
            .BindRuntimeType<UpdateSceneObjectInput>()
            .BindRuntimeType<UpdateSkillInput>()
            .BindRuntimeType<DependencyEdgeInput>()
            .BindRuntimeType<SceneObjectInput>()
            .BindRuntimeType<PositionTagInput>()
            .BindRuntimeType<PropertyInput>()
            .BindRuntimeType<PropertyTypeInput>()
            .BindRuntimeType<BooleanPropertyInput>()
            .BindRuntimeType<NumberPropertyInput>()
            .BindRuntimeType<StringPropertyInput>()
            .BindRuntimeType<PositionPropertyInput>()
            .BindRuntimeType<PositionTagPropertyInput>()
            .BindRuntimeType<SceneObjectPropertyInput>()
            .BindRuntimeType<NodeInput>()
            .BindRuntimeType<AgentInput>()
            .BindRuntimeType<SkillInput>()
            .ModifyOptions(o => { o.EnableOneOf = true; })
            .ModifyRequestOptions(o =>
            {
                // Enhanced error handling and debugging
                o.IncludeExceptionDetails = true;
                // Add timeout to prevent hanging requests
                o.ExecutionTimeout = TimeSpan.FromMinutes(2);
            })
            .AddErrorFilter<GraphQlErrorFilter>()
            .AddDiagnosticEventListener<GraphQlDiagnosticEventListener>()
            .AddInMemorySubscriptions()
            .ModifyCostOptions(options =>
            {
                // Read from configuration with defaults
                var costConfig = configuration.GetSection("GraphQL:Cost");
                options.MaxFieldCost = costConfig.GetValue("MaxFieldCost", 4_000);
                options.MaxTypeCost = costConfig.GetValue("MaxTypeCost", 1_000);
                options.EnforceCostLimits = costConfig.GetValue("EnforceCostLimits", true);
                options.ApplyCostDefaults = costConfig.GetValue("ApplyCostDefaults", true);
                options.DefaultResolverCost = costConfig.GetValue("DefaultResolverCost", 10.0);
            });

        return services;
    }
}