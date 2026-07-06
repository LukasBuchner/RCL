namespace FHOOE.Freydis.Application.Services.Variables.Exceptions;

/// <summary>
///     Base exception for variable-related errors.
/// </summary>
public class VariableException : Exception
{
    protected VariableException(string message) : base(message)
    {
    }

    protected VariableException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
///     Thrown when attempting to add a variable that already exists in a procedure.
/// </summary>
public class VariableAlreadyExistsException(string variableName, Guid procedureId)
    : VariableException($"Variable with name '{variableName}' already exists in procedure {procedureId}")
{
    public string VariableName { get; } = variableName;
    public Guid ProcedureId { get; } = procedureId;
}

/// <summary>
///     Thrown when a required variable is not found.
/// </summary>
public class VariableNotFoundException(string variableName)
    : VariableException($"Variable '{variableName}' not found in context.")
{
    public string VariableName { get; } = variableName;
}

/// <summary>
///     Thrown when variable value type doesn't match expected type.
/// </summary>
public class VariableTypeMismatchException(
    string variableName,
    Type expectedType,
    Type actualType)
    : VariableException(
        $"Variable '{variableName}' type mismatch. Expected: {expectedType.Name}, Actual: {actualType.Name}")
{
    public string VariableName { get; } = variableName;
    public Type ExpectedType { get; } = expectedType;
    public Type ActualType { get; } = actualType;
}

/// <summary>
///     Thrown when expression evaluation fails.
/// </summary>
public class ExpressionEvaluationException : VariableException
{
    public ExpressionEvaluationException(string expression, string message)
        : base($"Failed to evaluate expression '{expression}': {message}")
    {
        Expression = expression;
    }

    public ExpressionEvaluationException(string expression, Exception inner)
        : base($"Failed to evaluate expression '{expression}': {inner.Message}", inner)
    {
        Expression = expression;
    }

    public string Expression { get; }
}

/// <summary>
///     Thrown when multiple branches match in router (ambiguous condition).
/// </summary>
public class AmbiguousBranchException(List<string> matchingBranches) : VariableException(
    $"Multiple branches matched: {string.Join(", ", matchingBranches)}. Conditions must be mutually exclusive.")
{
    public List<string> MatchingBranches { get; } = matchingBranches;
}

/// <summary>
///     Thrown when no branch matches and no default branch exists.
/// </summary>
public class NoBranchMatchException(string routerName)
    : VariableException($"No branch matched in router '{routerName}' and no default branch exists.");