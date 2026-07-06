using FHOOE.Freydis.Agents.Agents.Kuka.Configuration;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace FHOOE.Freydis.Agents.Services.OpcUa;

/// <summary>
///     Manages OPC UA certificates for client connections.
/// </summary>
public class OpcUaCertificateManager(
    ILogger<OpcUaCertificateManager> logger,
    string? certificateStorePath = null)
{
    private readonly string _certificateStorePath = certificateStorePath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FHOOE.Freydis",
        "OpcUa",
        "PKI");

    /// <summary>
    ///     Gets or creates an application configuration with certificate management.
    /// </summary>
    public async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync(
        string applicationName,
        string applicationUri,
        OpcUaSecurityConfig securityConfig)
    {
        try
        {
            // Ensure directories exist
            EnsureCertificateDirectories();

            var config = new ApplicationConfiguration
            {
                ApplicationName = applicationName,
                ApplicationUri = applicationUri,
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(_certificateStorePath, "own"),
                        SubjectName = $"CN={applicationName}"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(_certificateStorePath, "trusted")
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(_certificateStorePath, "issuer")
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(_certificateStorePath, "rejected")
                    },
                    AutoAcceptUntrustedCertificates = securityConfig.AutoAcceptUntrustedCertificates,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024,
                    AddAppCertToTrustedStore = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            // Validate configuration
            await config.Validate(ApplicationType.Client);

            // Check/create certificate
            if (await config.SecurityConfiguration.ApplicationCertificate.Find(true) == null)
            {
                logger.LogCreatingSelfSignedOpcUaCertificate(applicationName);

                // Use the available API in this version
                var certificate = await config.SecurityConfiguration.ApplicationCertificate.LoadPrivateKeyEx(null);
                if (certificate == null)
                {
                    // Certificate still doesn't exist, create using CertificateFactory (with proper parameters)
#pragma warning disable CS0618 // Type or member is obsolete - no alternative available in this version
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
#pragma warning restore CS0618 // Type or member is obsolete

                    config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
                }

                logger.LogSelfSignedCertificateCreated(certificate.Subject, certificate.Thumbprint);
            }
            else
            {
                var existingCert = await config.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(null);
                logger.LogUsingExistingCertificateDetailed(
                    existingCert.Subject,
                    existingCert.Thumbprint,
                    existingCert.NotAfter);
            }

            if (securityConfig.AutoAcceptUntrustedCertificates)
                logger.LogAutoAcceptUntrustedCertificates();

            logger.LogCertificateConfigurationCompleted(_certificateStorePath);

            return config;
        }
        catch (Exception ex)
        {
            logger.LogApplicationConfigurationFailed(ex);
            throw;
        }
    }

    /// <summary>
    ///     Ensures all required certificate directories exist.
    /// </summary>
    private void EnsureCertificateDirectories()
    {
        var directories = new[]
        {
            Path.Combine(_certificateStorePath, "own", "certs"),
            Path.Combine(_certificateStorePath, "own", "private"),
            Path.Combine(_certificateStorePath, "trusted", "certs"),
            Path.Combine(_certificateStorePath, "trusted", "crl"),
            Path.Combine(_certificateStorePath, "issuer", "certs"),
            Path.Combine(_certificateStorePath, "issuer", "crl"),
            Path.Combine(_certificateStorePath, "rejected")
        };

        foreach (var directory in directories)
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                logger.LogCertificateDirectoryCreated(directory);
            }
    }

    /// <summary>
    ///     Gets the certificate store path.
    /// </summary>
    public string GetCertificateStorePath()
    {
        return _certificateStorePath;
    }
}