namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Abstract base class for all exceptions that represent a failed pre-condition check before
///     a procedure execution is allowed to begin. Every concrete subclass describes one category
///     of structural or runtime constraint that must hold prior to dispatch.
/// </summary>
/// <remarks>
///     A single <c>catch (ExecutionPreConditionException)</c> block in the GraphQL mutation layer
///     is sufficient to intercept any pre-condition failure and translate it into a structured
///     client error, using <see cref="ErrorCode" /> to discriminate the violation type and
///     <see cref="StructuredData" /> to surface machine-readable detail.
/// </remarks>
/// <param name="message">
///     Human-readable description of the constraint that was violated. Forwarded to
///     <see cref="InvalidOperationException" />.
/// </param>
public abstract class ExecutionPreConditionException(string message)
    : InvalidOperationException(message)
{
    /// <summary>
    ///     Short, uppercase, underscore-separated code that uniquely identifies the category of
    ///     pre-condition failure (e.g. <c>"AGENT_SERIALIZATION_VIOLATION"</c>).
    ///     Clients use this code to switch on the failure type without parsing the message string.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    ///     Optional machine-readable payload carrying structured detail about the violation.
    ///     Returns <see langword="null" /> by default; subclasses override this to expose
    ///     the specific records or collections that describe which constraints were broken
    ///     and on which entities.
    /// </summary>
    public virtual object? StructuredData => null;
}