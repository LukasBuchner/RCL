using System.Diagnostics;
using System.Globalization;
using Dapper;
using FHOOE.Freydis.Agents.Agents;
using FHOOE.Freydis.Agents.Services.Managers;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    ///     Adds health checks for the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("PostgreSQL",
                HealthStatus.Degraded,
                ["database", "postgresql"])
            .AddCheck<SystemHealthCheck>("System",
                HealthStatus.Degraded,
                ["system", "resources"])
            .AddCheck<AgentHealthCheck>("Agents",
                HealthStatus.Degraded,
                ["agents", "execution"])
            .AddCheck("GraphQL API",
                () => HealthCheckResult.Healthy("GraphQL endpoint is ready and accepting requests"),
                ["api", "graphql"]);

        // Register the health check implementations
        services.AddScoped<PostgresHealthCheck>();
        services.AddScoped<SystemHealthCheck>();
        services.AddScoped<AgentHealthCheck>();

        return services;
    }
}

/// <summary>
///     PostgreSQL health check implementation.
/// </summary>
public class PostgresHealthCheck(PostgresDbContext dbContext, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL") ?? "Not configured";
            data["ConnectionString"] = MaskConnectionString(connectionString);

            if (!dbContext.IsConnected)
            {
                data["Status"] = "Disconnected";
                data["Reason"] = "PostgreSQL context failed to initialize connection";
                data["Message"] = "PostgreSQL is unavailable. Server is running with limited functionality.";

                return HealthCheckResult.Degraded(
                    "PostgreSQL is unavailable but server is functional",
                    data: data);
            }

            await using var conn = dbContext.CreateConnection();
            await conn.OpenAsync(cancellationToken);

            var agentCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM agents");
            var skillCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM skills");
            var nodeCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM nodes");
            var edgeCount = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM dependency_edges");

            data["Tables"] = new
            {
                Agents = agentCount,
                Skills = skillCount,
                Nodes = nodeCount,
                DependencyEdges = edgeCount
            };

            data["Database"] = "FreydisDB";
            data["Status"] = "Connected";

            return HealthCheckResult.Healthy(
                "PostgreSQL is healthy and all tables are accessible",
                data);
        }
        catch (PostgresNotConnectedException ex)
        {
            data["Error"] = ex.Message;
            data["Status"] = "Disconnected";
            data["Message"] = "PostgreSQL operations are unavailable. Server is running with limited functionality.";

            return HealthCheckResult.Degraded(
                "PostgreSQL is unavailable but server is functional",
                data: data);
        }
        catch (Exception ex)
        {
            data["Error"] = ex.Message;
            data["Status"] = "Error";
            data["ErrorType"] = ex.GetType().Name;

            return HealthCheckResult.Unhealthy(
                "PostgreSQL health check failed with unexpected error",
                ex,
                data);
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";

        try
        {
            // Mask password in connection string
            var parts = connectionString.Split(';');
            var masked = parts.Select(p =>
                p.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase)
                    ? "Password=*****"
                    : p);
            return string.Join(";", masked);
        }
        catch
        {
            return "postgresql://*****";
        }
    }
}

/// <summary>
///     System health check implementation.
/// </summary>
public class SystemHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;

            data["Environment"] = Environment.OSVersion.ToString();
            data["RuntimeVersion"] = Environment.Version.ToString();
            data["MachineName"] = Environment.MachineName;
            data["ProcessorCount"] = Environment.ProcessorCount;
            data["MemoryUsage"] = new
            {
                ManagedMemoryMB = Math.Round(totalMemory / 1024.0 / 1024.0, 2),
                WorkingSetMB = Math.Round(workingSet / 1024.0 / 1024.0, 2),
                GCGeneration0 = GC.CollectionCount(0),
                GCGeneration1 = GC.CollectionCount(1),
                GCGeneration2 = GC.CollectionCount(2)
            };
            data["Uptime"] = TimeSpan.FromMilliseconds(Environment.TickCount64)
                .ToString(@"dd\.hh\:mm\:ss", CultureInfo.InvariantCulture);
            data["WorkingDirectory"] = Environment.CurrentDirectory;

            var memoryUsageMb = workingSet / 1024.0 / 1024.0;
            var status = memoryUsageMb switch
            {
                > 2000 => HealthStatus.Unhealthy,
                > 1000 => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            var message = status switch
            {
                HealthStatus.Healthy => "System is running optimally",
                HealthStatus.Degraded => "System is under moderate load",
                _ => "System is under heavy load"
            };

            return Task.FromResult(new HealthCheckResult(status, message, data: data));
        }
        catch (Exception ex)
        {
            data["Error"] = ex.Message;
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to retrieve system information", ex, data));
        }
    }
}

/// <summary>
///     Agent health check implementation that checks actual running agents.
/// </summary>
public partial class AgentHealthCheck : IHealthCheck
{
    private readonly IAgentManager _agentManager;
    private readonly ILogger<AgentHealthCheck> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="AgentHealthCheck" />.
    /// </summary>
    /// <param name="agentManager">The agent manager used to enumerate and query active agents.</param>
    /// <param name="logger">The logger for recording diagnostic and warning events.</param>
    public AgentHealthCheck(IAgentManager agentManager, ILogger<AgentHealthCheck> logger)
    {
        _agentManager = agentManager;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            var activeAgents = _agentManager.ActiveAgents;

            if (activeAgents.Count == 0)
            {
                data["RegisteredAgents"] = 0;
                data["HealthyAgents"] = 0;
                data["AvailableAgents"] = 0;
                data["TotalActiveExecutions"] = 0;
                data["Message"] = "No agents are currently registered or running";

                return new HealthCheckResult(
                    HealthStatus.Degraded,
                    "No agents are currently registered in the system",
                    data: data);
            }

            var agentHealthTasks = activeAgents.Select<IRuntimeAgent, Task<dynamic>>(async agent =>
            {
                try
                {
                    var health = await agent.GetHealthStatusAsync(cancellationToken);
                    return new
                    {
                        agent.Id,
                        agent.Name,
                        health.IsHealthy,
                        health.IsAvailable,
                        health.ActiveExecutions,
                        TotalExecutions = health.TotalExecutionsCompleted,
                        health.FailedExecutions,
                        SuccessRate = health.AdditionalMetrics?.ContainsKey("SuccessRate") == true
                            ? (double)health.AdditionalMetrics["SuccessRate"]
                            : 100.0,
                        UptimeHours = health.AdditionalMetrics?.ContainsKey("UptimeHours") == true
                            ? (double)health.AdditionalMetrics["UptimeHours"]
                            : 0.0,
                        CpuUsage = health.CpuUsagePercent ?? 0.0,
                        MemoryUsage = health.MemoryUsageMb ?? 0.0,
                        Status = health.StatusMessage,
                        LastSeen = health.LastSeenUtc,
                        StartedAt = health.StartedUtc
                    };
                }
                catch (Exception ex)
                {
                    LogAgentHealthCheckFailed(_logger, ex, agent.Id, agent.Name);
                    return new
                    {
                        agent.Id,
                        agent.Name,
                        IsHealthy = false,
                        IsAvailable = false,
                        ActiveExecutions = 0,
                        TotalExecutions = 0,
                        FailedExecutions = 0,
                        SuccessRate = 0.0,
                        UptimeHours = 0.0,
                        CpuUsage = 0.0,
                        MemoryUsage = 0.0,
                        Status = $"Health check failed: {ex.Message}",
                        LastSeen = (DateTime?)null,
                        StartedAt = (DateTime?)null
                    };
                }
            });

            var agents = await Task.WhenAll(agentHealthTasks);

            data["RegisteredAgents"] = agents.Length;
            data["HealthyAgents"] = agents.Count(a => a.IsHealthy);
            data["AvailableAgents"] = agents.Count(a => a.IsAvailable);
            data["TotalActiveExecutions"] = agents.Sum(a => a.ActiveExecutions);
            data["TotalExecutionsCompleted"] = agents.Sum(a => a.TotalExecutions);
            data["TotalFailedExecutions"] = agents.Sum(a => a.FailedExecutions);
            data["AverageSuccessRate"] = agents.Length > 0 ? agents.Average(a => (double?)a.SuccessRate ?? 0.0) : 0.0;
            data["AverageCpuUsage"] = agents.Length > 0 ? agents.Average(a => (double?)a.CpuUsage ?? 0.0) : 0.0;
            data["TotalMemoryUsage"] = agents.Sum(a => (double?)a.MemoryUsage ?? 0.0);
            data["AgentDetails"] = agents.Select(a => new
            {
                a.Id,
                a.Name,
                a.IsHealthy,
                a.IsAvailable,
                a.ActiveExecutions,
                a.TotalExecutions,
                a.FailedExecutions,
                SuccessRate = $"{a.SuccessRate:F1}%",
                UptimeHours = $"{a.UptimeHours:F2}h",
                CpuUsage = $"{a.CpuUsage:F1}%",
                MemoryUsage = $"{a.MemoryUsage:F1}MB",
                a.Status,
                a.LastSeen,
                a.StartedAt
            });

            var unhealthyAgents = agents.Count(a => !a.IsHealthy);
            var unavailableAgents = agents.Count(a => !a.IsAvailable);

            var status = (unhealthyAgents, unavailableAgents) switch
            {
                ( > 0, _) => HealthStatus.Unhealthy,
                (0, > 0) => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            var message = status switch
            {
                HealthStatus.Healthy => $"All {agents.Length} agents are healthy and available",
                HealthStatus.Degraded => $"{unavailableAgents} of {agents.Length} agents are unavailable",
                _ => $"{unhealthyAgents} of {agents.Length} agents are unhealthy"
            };

            return new HealthCheckResult(status, message, data: data);
        }
        catch (Exception ex)
        {
            data["Error"] = ex.Message;
            data["StackTrace"] = ex.StackTrace ?? "No stack trace available";
            return HealthCheckResult.Unhealthy("Failed to check agent health", ex, data);
        }
    }

    /// <summary>
    ///     Logs at Warning level that an individual agent's health probe threw, so a synthetic unhealthy record
    ///     with zeroed metrics is substituted for it in the aggregate health report.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception thrown by the agent's health probe.</param>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="agentName">The agent name.</param>
    [LoggerMessage(
        LogLevel.Warning,
        "Health check for agent {AgentId} ({AgentName}) failed; substituting a synthetic unhealthy record with zeroed metrics in the aggregate health report")]
    private static partial void LogAgentHealthCheckFailed(ILogger logger, Exception exception, Guid agentId,
        string agentName);
}