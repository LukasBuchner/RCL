namespace FHOOE.Freydis.GraphQLServer.Services.Initialization;

/// <summary>
///     Service responsible for initializing scene entities (position tags and scene objects).
/// </summary>
public interface ISceneInitializationService
{
    /// <summary>
    ///     Initializes scene entities from configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeSceneAsync(CancellationToken cancellationToken = default);
}