using FHOOE.Freydis.Agents.Agents.Kuka.Configuration;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace FHOOE.Freydis.Agents.Services.OpcUa;

/// <summary>
///     Manages OPC UA connection lifecycle for a single endpoint.
/// </summary>
public sealed class OpcUaConnectionManager(
    string endpoint,
    string applicationName,
    string applicationUri,
    OpcUaSecurityConfig securityConfig,
    ILogger<OpcUaConnectionManager> logger,
    OpcUaCertificateManager? certificateManager = null)
    : IOpcUaConnectionManager
{
    private readonly string _applicationName =
        applicationName ?? throw new ArgumentNullException(nameof(applicationName));

    private readonly string _applicationUri = applicationUri ?? throw new ArgumentNullException(nameof(applicationUri));
    private readonly string _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    private readonly ILogger<OpcUaConnectionManager>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly OpcUaSecurityConfig _securityConfig =
        securityConfig ?? throw new ArgumentNullException(nameof(securityConfig));

    private ApplicationConfiguration? _applicationConfiguration;
    private bool _disposed;

    public bool IsConnected => Session is { Connected: true };

    public ISession? Session { get; private set; }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnected)
        {
            _logger.LogAlreadyConnected(_endpoint);
            return;
        }

        try
        {
            if (!_endpoint.StartsWith("opc.tcp://", StringComparison.Ordinal))
                _logger.LogUnexpectedEndpointScheme(_endpoint);

            // Create or retrieve application configuration
            _applicationConfiguration = await CreateApplicationConfigurationAsync();

            // Select endpoint
            _logger.LogDiscoveringEndpoints(_endpoint, _securityConfig.UseEncryption);

            var endpointDescription = CoreClientUtils.SelectEndpoint(
                _applicationConfiguration,
                _endpoint,
                _securityConfig.UseEncryption);

            _logger.LogSelectedEndpoint(
                endpointDescription.EndpointUrl,
                endpointDescription.SecurityMode,
                endpointDescription.SecurityPolicyUri);

            // Create endpoint configuration
            var endpointConfiguration = EndpointConfiguration.Create(_applicationConfiguration);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            // Create user identity
            var userIdentity = CreateUserIdentity();

            // Create session
            _logger.LogCreatingSession();
            var sessionFactory = DefaultSessionFactory.Instance;
            Session = await sessionFactory.CreateAsync(
                _applicationConfiguration,
                endpoint,
                false, // updateBeforeConnect
                !_securityConfig.ValidateServerCertificate, // checkDomain
                _applicationName, // sessionName
                60000, // sessionTimeout
                userIdentity,
                null, // preferredLocales
                cancellationToken
            );

            _logger.LogConnectionSuccess(
                _endpoint,
                _securityConfig.UseAnonymousAuthentication ? "Anonymous" : "Authenticated",
                _securityConfig.UseEncryption ? "Enabled" : "Disabled",
                endpointDescription.SecurityMode);
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNotConnected)
        {
            _logger.LogConnectionUnreachable(_endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogConnectionFailed(ex, _endpoint);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (Session != null)
            {
                await Session.CloseAsync();
                Session.Dispose();
                Session = null;
                _logger.LogDisconnected(_endpoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDisconnectError(ex, _endpoint);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Session?.Close();
        Session?.Dispose();
        Session = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (Session != null)
        {
            await Session.CloseAsync();
            Session.Dispose();
            Session = null;
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private UserIdentity CreateUserIdentity()
    {
        if (_securityConfig.UseAnonymousAuthentication || string.IsNullOrEmpty(_securityConfig.Username))
        {
            _logger.LogUsingAnonymousAuth();
            return new UserIdentity(new AnonymousIdentityToken());
        }

        _logger.LogUsingUsernameAuth(_securityConfig.Username);
        if (_securityConfig.Password is null)
            _logger.LogMissingPasswordForUsernameAuth(_securityConfig.Username, _endpoint);
        return new UserIdentity(_securityConfig.Username, _securityConfig.Password ?? string.Empty);
    }

    private async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync()
    {
        // If certificate manager is provided, use it
        if (certificateManager != null)
        {
            _logger.LogUsingCertificateManager();
            return await certificateManager.CreateApplicationConfigurationAsync(
                _applicationName,
                _applicationUri,
                _securityConfig);
        }

        // Otherwise, create configuration manually
        var certificateStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FHOOE.Freydis",
            "OpcUa",
            "PKI");
        _logger.LogDefaultCertificateStorePath(certificateStorePath);

        var config = new ApplicationConfiguration
        {
            ApplicationName = _applicationName,
            ApplicationUri = _applicationUri,
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(certificateStorePath, "own"),
                    SubjectName = $"CN={_applicationName}"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(certificateStorePath, "trusted")
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(certificateStorePath, "issuer")
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(certificateStorePath, "rejected")
                },
                AutoAcceptUntrustedCertificates = _securityConfig.AutoAcceptUntrustedCertificates,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024,
                AddAppCertToTrustedStore = true
            },
            TransportConfigurations = [],
            TransportQuotas = new TransportQuotas { OperationTimeout = 1500000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TraceConfiguration = new TraceConfiguration()
        };

        await config.Validate(ApplicationType.Client);

        // Check/create certificate
        if (await config.SecurityConfiguration.ApplicationCertificate.Find(true) == null)
        {
            _logger.LogCreatingSelfSignedCertificate(config.ApplicationName);

            var certificate = await config.SecurityConfiguration.ApplicationCertificate.LoadPrivateKeyEx(null);
            if (certificate == null)
            {
#pragma warning disable CS0618
                certificate = CertificateFactory.CreateCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.StoreType,
                    config.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    config.ApplicationUri,
                    config.ApplicationName,
                    config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null,
                    2048,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    300,
                    256,
                    false,
                    null,
                    null);
#pragma warning restore CS0618

                config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            }

            _logger.LogCertificateCreated(certificate.Thumbprint);
        }
        else
        {
            _logger.LogUsingExistingCertificate(config.ApplicationName);
        }

        if (_securityConfig.AutoAcceptUntrustedCertificates)
            _logger.LogAutoAcceptUntrustedCertificates();

        if (!_securityConfig.ValidateServerCertificate)
            _logger.LogServerCertificateValidationDisabled();

        return config;
    }
}