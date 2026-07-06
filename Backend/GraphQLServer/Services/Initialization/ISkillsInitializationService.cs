namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Service responsible for initializing skill definitions.
/// </summary>
public interface ISkillsInitializationService
{
    /// <summary>
    ///     Initializes skill definitions from configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeSkillsAsync(CancellationToken cancellationToken = default);
}