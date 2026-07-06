using System.Diagnostics;
using FHOOE.Freydis.GraphQLServer.Support.Logging;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace FHOOE.Freydis.GraphQLServer.Services;

/// <summary>
///     Diagnostic event listener for GraphQL operations that provides detailed performance and execution logging.
/// </summary>
public class GraphQlDiagnosticEventListener(ILogger<GraphQlDiagnosticEventListener> logger)
    : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQlDiagnosticEventListener> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationType = context.Document?.ToString()?.Trim().Split(' ').FirstOrDefault()?.ToLowerInvariant();

        _logger.LogRequestStarted(
            operationType, context.ContextData.TryGetValue("requestId", out var reqId) ? reqId : "Unknown");

        return new RequestExecutionScope(_logger, stopwatch, operationType ?? "unknown");
    }

    public override void RequestError(IRequestContext context, Exception exception)
    {
        if (_logger.IsEnabled(LogLevel.Error))
            _logger.LogRequestError(
                exception,
                exception.Message,
                context.ContextData.TryGetValue("requestId", out var reqId) ? reqId : "Unknown",
                context.Document?.ToString());
    }

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        var path = error.Path?.Print() ?? "Unknown";
        var exceptionType = error.Exception?.GetType().Name ?? "No Exception";

        _logger.LogResolverError(
            context.Selection.Field.Name,
            error.Message,
            path,
            exceptionType);
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            var path = error.Path?.Print() ?? "Unknown";

            _logger.LogValidationError(
                error.Message,
                error.Code,
                path);
        }
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        if (_logger.IsEnabled(LogLevel.Error))
        {
            var locations = error.Locations?.Select(l => $"Line {l.Line}, Column {l.Column}").FirstOrDefault() ??
                            "Unknown";

            _logger.LogSyntaxError(
                error.Message,
                locations,
                context.Document?.ToString());
        }
    }

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var fieldName = $"{context.Selection.Field.DeclaringType.Name}.{context.Selection.Field.Name}";

        _logger.LogResolverStarted(fieldName);

        return new ResolverScope(_logger, stopwatch, fieldName);
    }

    private sealed class RequestExecutionScope(ILogger logger, Stopwatch stopwatch, string operationType) : IDisposable
    {
        public void Dispose()
        {
            stopwatch.Stop();
            logger.LogRequestCompleted(operationType, stopwatch.ElapsedMilliseconds);
        }
    }

    private sealed class ResolverScope(ILogger logger, Stopwatch stopwatch, string fieldName) : IDisposable
    {
        public void Dispose()
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100) // Only log slow resolvers
                logger.LogSlowResolver(fieldName, stopwatch.ElapsedMilliseconds);
            else
                logger.LogResolverCompleted(fieldName, stopwatch.ElapsedMilliseconds);
        }
    }
}