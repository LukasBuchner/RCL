using System.ComponentModel.DataAnnotations;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for options validation.
/// </summary>
public static class OptionsValidationExtensions
{
    /// <summary>
    ///     Adds configuration validation to ensure required settings are present.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate PostgreSQL connection string
        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection("ConnectionStrings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Validate GraphQL cost options
        services.AddOptions<GraphQlCostOptions>()
            .Bind(configuration.GetSection("GraphQL:Cost"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Validate CORS options
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection("Cors"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}

/// <summary>
///     Options for PostgreSQL configuration.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    ///     PostgreSQL connection string.
    /// </summary>
    [Required(ErrorMessage = "PostgreSQL connection string is required")]
    public string PostgreSql { get; set; } = string.Empty;
}

/// <summary>
///     Options for GraphQL cost configuration.
/// </summary>
public class GraphQlCostOptions
{
    /// <summary>
    ///     Maximum field cost allowed.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxFieldCost must be greater than 0")]
    public int MaxFieldCost { get; set; } = 4000;

    /// <summary>
    ///     Maximum type cost allowed.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxTypeCost must be greater than 0")]
    public int MaxTypeCost { get; set; } = 1000;

    /// <summary>
    ///     Whether to enforce cost limits.
    /// </summary>
    public bool EnforceCostLimits { get; set; } = true;

    /// <summary>
    ///     Whether to apply cost defaults.
    /// </summary>
    public bool ApplyCostDefaults { get; set; } = true;

    /// <summary>
    ///     Default resolver cost.
    /// </summary>
    [Range(0.1, double.MaxValue, ErrorMessage = "DefaultResolverCost must be greater than 0")]
    public double DefaultResolverCost { get; set; } = 10.0;
}

/// <summary>
///     Options for CORS configuration.
/// </summary>
public class CorsOptions
{
    /// <summary>
    ///     Allowed origins for CORS.
    /// </summary>
    [Required(ErrorMessage = "At least one allowed origin must be specified")]
    public string[] AllowedOrigins { get; set; } = [];
}