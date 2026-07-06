using FHOOE.Freydis.Application.Services.Variables.Exceptions;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;

namespace FHOOE.Freydis.Application.Services.Branching;

/// <summary>
///     Service for selecting branches in RouterNodes based on selector expressions.
/// </summary>
public interface IBranchSelector
{
    /// <summary>
    ///     Selects exactly one branch from a RouterNode based on the selector expression.
    /// </summary>
    /// <param name="router">The RouterNode to select a branch from.</param>
    /// <param name="context">The variable context for evaluating conditions.</param>
    /// <returns>The selected ConditionalBranch.</returns>
    /// <exception cref="InvalidOperationException">Thrown when router is invalid.</exception>
    /// <exception cref="AmbiguousBranchException">Thrown when multiple branches match with same priority.</exception>
    /// <exception cref="NoBranchMatchException">Thrown when no branches match and no default exists.</exception>
    Task<ConditionalBranch> SelectBranchAsync(
        RouterNode router,
        VariableContext context);
}