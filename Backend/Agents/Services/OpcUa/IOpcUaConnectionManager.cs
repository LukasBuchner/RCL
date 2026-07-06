using Opc.Ua.Client;

namespace FHOOE.Freydis.Agents.Services.OpcUa;

/// <summary>
///     Manages OPC UA connection lifecycle for a single endpoint.
/// </summary>
public interface IOpcUaConnectionManager : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Gets a value indicating whether the OPC UA session is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets the current OPC UA session, or null if not connected.
    /// </summary>
    ISession? Session { get; }

    /// <summary>
    ///     Asynchronously connects to the OPC UA server using configured endpoint and security settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously disconnects from the OPC UA server and cleans up session resources.
    /// </summary>
    /// <returns>A task representing the asynchronous disconnection operation.</returns>
    Task DisconnectAsync();
}