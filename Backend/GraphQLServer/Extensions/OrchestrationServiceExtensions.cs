using System.Reactive.Concurrency;
using FHOOE.Freydis.Application.Services.Common.Platform;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding;
using FHOOE.Freydis.Application.Services.Scheduling.GraphBuilding.Utils;
using FHOOE.Freydis.Application.Services.Scheduling.Planning;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring orchestration services.
/// </summary>
public static class OrchestrationServiceExtensions
{
    /// <summary>
    ///     Adds orchestration services including graph building, scheduling, and execution.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestrationServices(this IServiceCollection services)
    {
        // Graph building services - Changed to Singleton to support singleton entity services
        // NOTE: INodeAgentMapper and IAgentCapabilityAnalyzer are registered in ApplicationServiceExtensions (single source of truth)
        services.AddSingleton<IEdgeTypeMapper, EdgeTypeMapper>();
        services.AddSingleton<IExecutionGraphBuilder, ExecutionGraphBuilder>();

        // Schedule planning service - Required by other services even though ExecutionOrchestrator doesn't use it directly
        services.AddSingleton<ISchedulePlanner, SchedulePlanner>();

        // For IScheduler, we'll use a factory to ensure proper lifetime management
        services.AddSingleton<IScheduler>(_ => Scheduler.Default);
        services.AddSingleton<TimeProvider>(_ => TimeProvider.System);

        // Windows timer resolution fix — raises system timer resolution from ~15.6ms to 1ms
        // so that Rx.NET timers (Sample, Timer, etc.) fire at their requested intervals
        services.AddHostedService<WindowsTimerResolutionService>();

        // Execution orchestrator is registered in ApplicationServiceExtensions.cs
        // DO NOT register here to avoid duplicate registration

        return services;
    }
}