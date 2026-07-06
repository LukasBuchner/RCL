using System.Diagnostics;
using FHOOE.Freydis.Application.Services.EntityManagement.Agents;
using FHOOE.Freydis.Application.Services.EntityManagement.Edges;
using FHOOE.Freydis.Application.Services.EntityManagement.Nodes;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.EntityManagement.Skills;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;
using HotChocolate.Execution;

namespace FHOOE.Freydis.GraphQLServer.Services;

/// <summary>
///     Service that validates the application configuration and dependencies at startup.
///     This ensures that all required services are properly configured before the first GraphQL request.
/// </summary>
public class StartupValidationService(
    IServiceProvider serviceProvider,
    ILogger<StartupValidationService> logger,
    IHostEnvironment environment)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogStartingValidation();

        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();

        try
        {
            // Validate PostgreSQL connection
            await ValidatePostgresConnectionAsync(errors);

            // Validate required services are registered
            ValidateServiceRegistrations(errors);

            // Validate GraphQL schema
            await ValidateGraphQlSchemaAsync(errors);

            // Validate application services
            await ValidateApplicationServicesAsync(errors);

            stopwatch.Stop();

            if (errors.Count > 0)
            {
                var formattedErrors = string.Join("\n", errors.Select(e => $"  - {e}"));
                logger.LogValidationFailed(errors.Count, formattedErrors);

                // In development or when debugging, fail fast
                if (environment.IsDevelopment() || Debugger.IsAttached)
                {
                    var errorMessage = $"Startup validation failed with {errors.Count} error(s):\n{formattedErrors}";
                    throw new InvalidOperationException($"Application startup validation failed:\n{errorMessage}");
                }
            }
            else
            {
                logger.LogValidationCompleted(stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogUnexpectedValidationError(ex);

            if (environment.IsDevelopment() || Debugger.IsAttached)
                throw new InvalidOperationException("Application startup validation encountered an unexpected error",
                    ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ValidatePostgresConnectionAsync(List<string> errors)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var postgresContext = scope.ServiceProvider.GetService<PostgresDbContext>();

            if (postgresContext == null)
            {
                errors.Add("PostgreSQL context is not registered");
                return;
            }

            // Try to open a connection to validate connectivity
            try
            {
                await using var connection = postgresContext.CreateConnection();
                await connection.OpenAsync();
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"PostgreSQL connectivity test failed: {ex.Message}");
                return;
            }

            logger.LogPostgresValidated();
        }
        catch (Exception ex)
        {
            errors.Add($"PostgreSQL connection validation failed: {ex.Message}");
        }
    }

    private void ValidateServiceRegistrations(List<string> errors)
    {
        var requiredServices = new[]
        {
            // Application Services
            typeof(INodeApplicationService),
            typeof(IDependencyEdgeApplicationService),
            typeof(IAgentApplicationService),
            typeof(ISkillApplicationService),
            typeof(IPositionTagApplicationService),
            typeof(ISceneObjectApplicationService),

            // GraphQL Services
            typeof(IGraphQlMapperService),

            // Infrastructure
            typeof(PostgresDbContext)
        };

        using var scope = serviceProvider.CreateScope();

        foreach (var serviceType in requiredServices)
            try
            {
                var service = scope.ServiceProvider.GetService(serviceType);
                if (service == null) errors.Add($"Required service {serviceType.Name} is not registered");
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to resolve service {serviceType.Name}: {ex.Message}");
            }

        logger.LogServiceValidationCompleted(errors.Count);
    }

    private async Task ValidateGraphQlSchemaAsync(List<string> errors)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var executor = scope.ServiceProvider.GetService<IRequestExecutorResolver>();

            if (executor == null)
            {
                errors.Add("GraphQL executionOrchestrator resolver is not registered");
                return;
            }

            // Get the default schema executionOrchestrator
            var requestExecutor = await executor.GetRequestExecutorAsync(cancellationToken: default);

            if (requestExecutor == null)
            {
                errors.Add("GraphQL request executionOrchestrator could not be resolved");
                return;
            }

            // Validate that the schema is built
            var schema = requestExecutor.Schema;
            if (schema == null)
            {
                errors.Add("GraphQL schema is not built");
                return;
            }

            // Check for critical types - use concrete type checking
            var criticalTypes = new[] { "Query", "Mutation", "Subscription", "Agent", "Skill", "Node" };
            foreach (var typeName in criticalTypes)
            {
                var type = schema.Types.FirstOrDefault(t => t.Name == typeName);
                if (type == null) errors.Add($"GraphQL schema is missing critical type: {typeName}");
            }

            logger.LogSchemaValidated();
        }
        catch (Exception ex)
        {
            errors.Add($"GraphQL schema validation failed: {ex.Message}");
            if (ex.InnerException != null) errors.Add($"  Inner exception: {ex.InnerException.Message}");
            logger.LogSchemaValidationError(ex);
        }
    }

    private async Task ValidateApplicationServicesAsync(List<string> errors)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            // Test that we can retrieve data from each service
            var agentService = scope.ServiceProvider.GetService<IAgentApplicationService>();
            if (agentService != null)
                try
                {
                    // This should not throw even if no agents exist
                    var agents = await agentService.GetAllAgentsAsync();
                    logger.LogAgentServiceValidated(agents.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Agent service validation failed: {ex.Message}");
                }

            var skillService = scope.ServiceProvider.GetService<ISkillApplicationService>();
            if (skillService != null)
                try
                {
                    var skills = await skillService.GetAllSkillsAsync();
                    logger.LogSkillServiceValidated(skills.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Skill service validation failed: {ex.Message}");
                }

            var nodeService = scope.ServiceProvider.GetService<INodeApplicationService>();
            if (nodeService != null)
                try
                {
                    var nodes = await nodeService.GetAllNodesAsync();
                    logger.LogNodeServiceValidated(nodes.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Node service validation failed: {ex.Message}");
                }
        }
        catch (Exception ex)
        {
            errors.Add($"Application services validation failed: {ex.Message}");
        }
    }
}