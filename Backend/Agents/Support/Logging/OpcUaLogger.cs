using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace FHOOE.Freydis.Agents.Support.Logging;

/// <summary>
///     Provides structured logging for OPC UA connection and certificate management operations
///     using high-performance source-generated logging.
/// </summary>
public static partial class OpcUaLogger
{
    // ── OpcUaConnectionManager ──────────────────────────────────────────

    /// <summary>
    ///     Logs a warning when a connection attempt is made to a server that is already connected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Already connected to OPC UA server at {Endpoint}")]
    public static partial void LogAlreadyConnected(this ILogger logger, string endpoint);

    /// <summary>
    ///     Logs a warning when the endpoint URL does not use the expected <c>opc.tcp://</c> scheme.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL with the unexpected scheme.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Endpoint URL doesn't use opc.tcp:// scheme: {Endpoint}")]
    public static partial void LogUnexpectedEndpointScheme(this ILogger logger, string endpoint);

    /// <summary>
    ///     Logs the start of endpoint discovery, including whether encryption is requested.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL being discovered.</param>
    /// <param name="useEncryption">Whether encryption is requested for the connection.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Discovering endpoints at {Endpoint} (UseEncryption: {UseEncryption})")]
    public static partial void LogDiscoveringEndpoints(this ILogger logger, string endpoint, bool useEncryption);

    /// <summary>
    ///     Logs details of the selected OPC UA endpoint after discovery, including the resolved URL,
    ///     security mode, and security policy URI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="url">The resolved endpoint URL.</param>
    /// <param name="securityMode">The negotiated security mode of the endpoint.</param>
    /// <param name="securityPolicy">The security policy URI of the endpoint.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Selected endpoint: URL={Url}, SecurityMode={SecurityMode}, SecurityPolicy={SecurityPolicy}")]
    public static partial void LogSelectedEndpoint(
        this ILogger logger,
        string url,
        MessageSecurityMode securityMode,
        string securityPolicy);

    /// <summary>
    ///     Logs that an OPC UA session is being created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Creating OPC UA session...")]
    public static partial void LogCreatingSession(this ILogger logger);

    /// <summary>
    ///     Logs a successful OPC UA connection including authentication mode, encryption status, and security mode.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL.</param>
    /// <param name="authMode">The authentication mode used (e.g., "Anonymous" or "Authenticated").</param>
    /// <param name="encryption">The encryption status (e.g., "Enabled" or "Disabled").</param>
    /// <param name="securityMode">The negotiated security mode of the endpoint.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Successfully connected to OPC UA server at {Endpoint} (Auth: {AuthMode}, Encryption: {Encryption}, SecurityMode: {SecurityMode})")]
    public static partial void LogConnectionSuccess(
        this ILogger logger,
        string endpoint,
        string authMode,
        string encryption,
        MessageSecurityMode securityMode);

    /// <summary>
    ///     Logs an error when a connection attempt fails with a <c>BadNotConnected</c> status,
    ///     indicating the server may be unreachable.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL that could not be reached.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message =
            "Unable to connect to OPC UA server at {Endpoint}. Verify the server is running, the endpoint URL is correct, the port is open, and no firewall is blocking the connection")]
    public static partial void LogConnectionUnreachable(this ILogger logger, string endpoint);

    /// <summary>
    ///     Logs a general connection failure with the associated exception details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the connection failure.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to OPC UA server at {Endpoint}")]
    public static partial void LogConnectionFailed(this ILogger logger, Exception exception, string endpoint);

    /// <summary>
    ///     Logs a successful disconnection from an OPC UA server.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL that was disconnected.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Disconnected from OPC UA server at {Endpoint}")]
    public static partial void LogDisconnected(this ILogger logger, string endpoint);

    /// <summary>
    ///     Logs an error that occurred during disconnection from an OPC UA server.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred during disconnection.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Error while disconnecting from OPC UA server at {Endpoint}")]
    public static partial void LogDisconnectError(this ILogger logger, Exception exception, string endpoint);

    /// <summary>
    ///     Logs that anonymous authentication is being used for the OPC UA session.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Using anonymous authentication")]
    public static partial void LogUsingAnonymousAuth(this ILogger logger);

    /// <summary>
    ///     Logs that username/password authentication is being used, including the username.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="username">The username used for authentication.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Using username/password authentication for user: {Username}")]
    public static partial void LogUsingUsernameAuth(this ILogger logger, string username);

    /// <summary>
    ///     Logs a warning when username authentication is configured but no password was supplied.
    ///     An empty string is substituted for the missing password, which will typically cause
    ///     authentication to fail against a real OPC UA server.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="username">The username for which the password is absent.</param>
    /// <param name="endpoint">The OPC UA server endpoint URL where authentication will be attempted.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Username authentication configured for user {Username} but no password supplied; falling back to empty password (authentication will likely fail at {Endpoint})")]
    public static partial void LogMissingPasswordForUsernameAuth(this ILogger logger, string username, string endpoint);

    /// <summary>
    ///     Logs that the certificate manager is being used to create the application configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Using certificate manager for application configuration")]
    public static partial void LogUsingCertificateManager(this ILogger logger);

    /// <summary>
    ///     Logs the default certificate store path being used when no certificate manager is configured.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The filesystem path to the certificate store.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Using default certificate store path: {Path}")]
    public static partial void LogDefaultCertificateStorePath(this ILogger logger, string path);

    // ── Shared (used by both ConnectionManager and CertificateManager) ──

    /// <summary>
    ///     Logs the start of self-signed certificate creation for a given application.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="applicationName">The OPC UA application name the certificate is being created for.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating self-signed certificate for {ApplicationName}")]
    public static partial void LogCreatingSelfSignedCertificate(this ILogger logger, string applicationName);

    /// <summary>
    ///     Logs that a self-signed certificate was successfully created, including its thumbprint.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="thumbprint">The SHA-1 thumbprint of the created certificate.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Certificate created successfully: Thumbprint={Thumbprint}")]
    public static partial void LogCertificateCreated(this ILogger logger, string thumbprint);

    /// <summary>
    ///     Logs that an existing certificate is being reused for a given application (no subject/thumbprint details).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="applicationName">The OPC UA application name whose existing certificate is being reused.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Using existing certificate for {ApplicationName}")]
    public static partial void LogUsingExistingCertificate(this ILogger logger, string applicationName);

    /// <summary>
    ///     Logs a warning that untrusted certificates are being automatically accepted.
    ///     This configuration is not recommended for production environments.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Auto-accepting untrusted certificates. This is NOT recommended for production environments!")]
    public static partial void LogAutoAcceptUntrustedCertificates(this ILogger logger);

    /// <summary>
    ///     Logs a warning that server certificate domain validation is disabled.
    ///     This configuration is not recommended for production environments.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message =
            "Server certificate domain validation is DISABLED. This is NOT recommended for production environments!")]
    public static partial void LogServerCertificateValidationDisabled(this ILogger logger);

    // ── OpcUaCertificateManager ─────────────────────────────────────────

    /// <summary>
    ///     Logs the start of self-signed OPC UA certificate creation, indicating the application name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="applicationName">The OPC UA application name the certificate is being created for.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Creating self-signed OPC UA certificate for {ApplicationName}")]
    public static partial void LogCreatingSelfSignedOpcUaCertificate(this ILogger logger, string applicationName);

    /// <summary>
    ///     Logs that a self-signed certificate was created, including its subject and thumbprint.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="subject">The subject distinguished name of the created certificate.</param>
    /// <param name="thumbprint">The SHA-1 thumbprint of the created certificate.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Created self-signed certificate: Subject={Subject}, Thumbprint={Thumbprint}")]
    public static partial void LogSelfSignedCertificateCreated(this ILogger logger, string subject, string thumbprint);

    /// <summary>
    ///     Logs that an existing certificate is being reused, including its subject, thumbprint, and expiry date.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="subject">The subject distinguished name of the existing certificate.</param>
    /// <param name="thumbprint">The SHA-1 thumbprint of the existing certificate.</param>
    /// <param name="expiry">The expiration date of the existing certificate.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Using existing certificate: Subject={Subject}, Thumbprint={Thumbprint}, Expires={Expiry}")]
    public static partial void LogUsingExistingCertificateDetailed(
        this ILogger logger,
        string subject,
        string thumbprint,
        DateTime expiry);

    /// <summary>
    ///     Logs that certificate configuration has been completed, including the store path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The filesystem path to the certificate store.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Certificate configuration completed. Store path: {Path}")]
    public static partial void LogCertificateConfigurationCompleted(this ILogger logger, string path);

    /// <summary>
    ///     Logs a failure to create the OPC UA application configuration with certificates.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the configuration failure.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create application configuration with certificates")]
    public static partial void LogApplicationConfigurationFailed(this ILogger logger, Exception exception);

    /// <summary>
    ///     Logs the creation of a certificate directory that did not previously exist.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="directory">The filesystem path of the created directory.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Created certificate directory: {Directory}")]
    public static partial void LogCertificateDirectoryCreated(this ILogger logger, string directory);
}