namespace FHOOE.Freydis.Infrastructure.Tests;

/// <summary>
///     Custom xUnit <see cref="FactAttribute" /> that skips integration tests by default.
///     Tests annotated with this attribute are only executed when the
///     <c>RUN_INTEGRATION_TESTS</c> environment variable is set to <c>true</c>.
///     This prevents integration tests (which require external dependencies like PostgreSQL)
///     from failing in environments where those dependencies are unavailable.
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    /// <summary>
    ///     Initializes a new instance of <see cref="IntegrationFactAttribute" />.
    ///     Automatically sets <see cref="FactAttribute.Skip" /> with a descriptive message
    ///     unless the <c>RUN_INTEGRATION_TESTS</c> environment variable is <c>true</c>.
    /// </summary>
    public IntegrationFactAttribute()
    {
        var runIntegration = Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS");

        if (!string.Equals(runIntegration, "true", StringComparison.OrdinalIgnoreCase))
            Skip = "Integration test skipped. Set RUN_INTEGRATION_TESTS=true to run.";
    }
}