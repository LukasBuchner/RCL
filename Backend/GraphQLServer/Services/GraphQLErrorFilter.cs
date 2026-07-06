using System.Diagnostics;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Services;

/// <summary>
///     Enhanced error filter for GraphQL operations that provides detailed error information and logging.
/// </summary>
public class GraphQlErrorFilter(
    ILogger<GraphQlErrorFilter> logger,
    IHostEnvironment environment)
    : IErrorFilter
{
    private readonly IHostEnvironment
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    private readonly ILogger<GraphQlErrorFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IError OnError(IError error)
    {
        // Log all GraphQL errors with detailed information
        LogGraphQlError(error);

        var errorId = Guid.NewGuid().ToString();
        var enhancedError = error
            .SetExtension("timestamp", DateTimeOffset.UtcNow.ToString("O"))
            .SetExtension("errorId", errorId);

        // In development or debug mode, include full exception details
        if (_environment.IsDevelopment() || Debugger.IsAttached)
        {
            if (error.Exception != null)
            {
                enhancedError = enhancedError
                    .SetExtension("exceptionType", error.Exception.GetType().FullName)
                    .SetExtension("stackTrace", error.Exception.StackTrace);

                // Check for common resolver issues
                if (error.Exception is InvalidOperationException ioe)
                    enhancedError = enhancedError.WithMessage(
                        $"Resolver error: {ioe.Message}. This typically indicates a missing service registration or configuration issue.");
                else if (error.Exception is NullReferenceException)
                    enhancedError = enhancedError.WithMessage(
                        "Null reference in resolver. Check that all required services are properly injected and initialized.");
            }
        }
        else
        {
            // In production, sanitize error messages
            if (error.Exception != null)
                enhancedError = enhancedError.WithMessage(
                    $"An internal error occurred. Error ID: {errorId}");
        }

        return enhancedError;
    }

    private void LogGraphQlError(IError error)
    {
        var path = error.Path?.Print() ?? "Unknown";
        var locations = error.Locations?.Select(l => $"Line {l.Line}, Column {l.Column}").FirstOrDefault() ?? "Unknown";

        if (error.Exception != null)
            _logger.LogGraphQlErrorWithException(
                error.Exception,
                error.Message,
                error.Code,
                path,
                locations,
                error.Extensions);
        else
            _logger.LogGraphQlWarning(
                error.Message,
                error.Code,
                path,
                locations,
                error.Extensions);

        // Log additional debug information — guard to avoid constructing the anonymous object when not needed
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var errorInfo = new
            {
                error.Message,
                error.Code,
                Path = error.Path?.ToList(),
                Locations = error.Locations?.Select(l => new { l.Line, l.Column }).ToList(),
                error.Extensions,
                Exception = error.Exception?.GetType().Name,
                ExceptionMessage = error.Exception?.Message,
                error.Exception?.StackTrace
            };

            _logger.LogGraphQlErrorDetails(errorInfo);
        }
    }
}