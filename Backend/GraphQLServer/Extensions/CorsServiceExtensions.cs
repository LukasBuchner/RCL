using Serilog;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring CORS.
/// </summary>
public static class CorsServiceExtensions
{
    /// <summary>
    ///     Adds CORS configuration from app settings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var configuredOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>();
        var allowedOrigins = configuredOrigins ?? ["http://localhost:5173", "http://localhost:5174"];
        if (configuredOrigins is null)
            Log.Warning(
                "CORS allowed-origins configuration section 'Cors:AllowedOrigins' was missing or unbindable; falling back to hardcoded development origins {FallbackOrigins}",
                (object)allowedOrigins);

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}