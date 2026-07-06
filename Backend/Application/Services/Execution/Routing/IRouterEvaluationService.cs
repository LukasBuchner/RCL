using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Routing;

/// <summary>
///     Service for evaluating RouterNodes during execution.
///     Determines which branch to take based on current variable values.
/// </summary>
public interface IRouterEvaluationService
{
    /// <summary>
    ///     Evaluates a RouterNode and returns the selected branch's target node ID.
    /// </summary>
    /// <param name="routerNode">The RouterNode to evaluate.</param>
    /// <param name="context">Current variable context.</param>
    /// <returns>The ID of the node to execute (selected branch's target).</returns>
    /// <exception cref="ArgumentNullException">Thrown when routerNode or context is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when RouterNode is not valid.</exception>
    /// <exception cref="VariableNotFoundException">Thrown when required variable is not found in context.</exception>
    /// <exception cref="AmbiguousBranchException">Thrown when multiple branches match at the same priority.</exception>
    /// <exception cref="NoBranchMatchException">Thrown when no branch matches and there's no default.</exception>
    Task<Guid> EvaluateRouterAsync(RouterNode routerNode, VariableContextEntity context);
}