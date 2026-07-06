using System.Globalization;
using DynamicExpresso;
using DynamicExpresso.Exceptions;
using FHOOE.Freydis.Application.Services.Variables.Exceptions;

namespace FHOOE.Freydis.Application.Services.Expressions;

/// <summary>
///     Service for evaluating expressions safely using DynamicExpresso.
/// </summary>
public class ExpressionEvaluator : IExpressionEvaluator
{
    private readonly Interpreter _interpreter;
    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(500);

    public ExpressionEvaluator()
    {
        _interpreter = new Interpreter();

        // Configure safe interpreter
        ConfigureSafeInterpreter();
    }

    public async Task<object?> EvaluateAsync(
        string expression,
        Dictionary<string, object?> context)
    {
        try
        {
            var parameters = context
                .Select(kvp => new Parameter(kvp.Key, kvp.Value?.GetType() ?? typeof(object), kvp.Value))
                .ToArray();

            // Execute with timeout — WaitAsync enforces the deadline on the result,
            // not on threadpool scheduling, so CI under load won't false-positive.
            var task = Task.Run(() => _interpreter.Eval(expression, parameters));

            return await task.WaitAsync(_timeout);
        }
        catch (TimeoutException)
        {
            throw new ExpressionEvaluationException(
                expression,
                $"Expression evaluation timeout (max {_timeout.TotalMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            throw new ExpressionEvaluationException(expression, ex);
        }
    }

    public async Task<bool> EvaluateBooleanAsync(
        string expression,
        Dictionary<string, object?> context)
    {
        var result = await EvaluateAsync(expression, context);

        if (result is bool boolResult)
            return boolResult;

        throw new ExpressionEvaluationException(
            expression,
            $"Expression did not return boolean value. Returned: {result?.GetType().Name ?? "null"}");
    }

    public bool ValidateSyntax(string expression, out string? errorMessage)
    {
        try
        {
            // Parse the expression to check syntax
            // Note: Parse will throw for both syntax errors AND unknown identifiers
            _interpreter.Parse(expression, typeof(object));

            errorMessage = null;
            return true;
        }
        catch (UnknownIdentifierException)
        {
            // Undefined variable is OK for syntax validation — we only care about structural errors
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private void ConfigureSafeInterpreter()
    {
        // Whitelist safe Math functions
        _interpreter.Reference(typeof(Math));
        _interpreter.SetFunction("abs", (Func<double, double>)Math.Abs);
        _interpreter.SetFunction("max", (Func<double, double, double>)Math.Max);
        _interpreter.SetFunction("min", (Func<double, double, double>)Math.Min);
        _interpreter.SetFunction("round", (Func<double, double>)Math.Round);

        // Whitelist safe string functions
        _interpreter.SetFunction("upper", (Func<string, string>)(s => s.ToUpper(CultureInfo.InvariantCulture)));
        _interpreter.SetFunction("lower", (Func<string, string>)(s => s.ToLower(CultureInfo.InvariantCulture)));
        _interpreter.SetFunction("trim", (Func<string, string>)(s => s.Trim()));
    }
}