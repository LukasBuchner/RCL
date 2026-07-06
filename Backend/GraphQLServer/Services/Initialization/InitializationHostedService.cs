using FHOOE.Freydis.GraphQLServer.Support.Logging;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Hosted service that initializes scene entities and agents during application startup.
/// </summary>
public class InitializationHostedService : IHostedService
{
    private readonly ILogger<InitializationHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the InitializationHostedService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="logger">Logger instance.</param>
    public InitializationHostedService(
        IServiceProvider serviceProvider,
        ILogger<InitializationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    ///     Starts the scene, skills, and agent initialization process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogStartingInitialization();

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();

            _logger.LogResolvingServices();

            // Initialize scene entities first (position tags, scene objects)
            try
            {
                var sceneInitializationService =
                    scope.ServiceProvider.GetRequiredService<ISceneInitializationService>();
                await sceneInitializationService.InitializeSceneAsync(cancellationToken);
                _logger.LogSceneCompleted();
            }
            catch (PostgresNotConnectedException)
            {
                _logger.LogSkippingScene();
            }
            catch (Exception ex)
            {
                _logger.LogSceneFailed(ex);
            }

            // Initialize skill definitions (does not require PostgreSQL)
            try
            {
                var skillsInitializationService =
                    scope.ServiceProvider.GetRequiredService<ISkillsInitializationService>();
                await skillsInitializationService.InitializeSkillsAsync(cancellationToken);
                _logger.LogSkillsCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogSkillsFailed(ex);
            }

            // Then initialize agents (which depend on scene entities and skills)
            try
            {
                var agentStartupService = scope.ServiceProvider.GetRequiredService<IAgentStartupService>();
                await agentStartupService.InitializeAgentsAsync(cancellationToken);
                _logger.LogAgentsCompleted();
            }
            catch (PostgresNotConnectedException)
            {
                _logger.LogSkippingAgents();
            }
            catch (Exception ex)
            {
                _logger.LogAgentsFailed(ex);
            }

            _logger.LogInitializationCompleted();
        }
        catch (Exception ex)
        {
            _logger.LogInitializationCriticalError(ex);
            throw;
        }
    }

    /// <summary>
    ///     Stops the hosted service (no cleanup needed for agent initialization).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogServiceStopping();
        return Task.CompletedTask;
    }
}