using FHOOE.Freydis.Application.Services.Branching;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using VariableContextEntity = FHOOE.Freydis.Domain.Entities.Variables.VariableContext;

namespace FHOOE.Freydis.Application.Services.Execution.Routing;

/// <summary>
///     Service for evaluating RouterNodes during execution.
///     Uses IBranchSelector to determine which branch to take based on current variable values.
///     Router selections are tracked in-memory only during execution to prevent execution-time
///     timing values from being persisted to the database and contaminating subsequent schedules.
/// </summary>
public class RouterEvaluationService : IRouterEvaluationService
{
    private readonly IBranchSelector _branchSelector;
    private readonly ILogger<RouterEvaluationService> _logger;

    /// <summary>
    ///     Initializes a new instance of the RouterEvaluationService class.
    /// </summary>
    /// <param name="branchSelector">Service for selecting branches.</param>
    /// <param name="logger">Logger for router evaluation operations.</param>
    public RouterEvaluationService(
        IBranchSelector branchSelector,
        ILogger<RouterEvaluationService> logger)
    {
        _branchSelector = branchSelector ?? throw new ArgumentNullException(nameof(branchSelector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Evaluates a RouterNode and returns the selected branch's target node ID.
    ///     The selection is kept in-memory only; the execution trigger service tracks it
    ///     via <c>GetRouterSelections()</c> for rescheduling and branch filtering.
    /// </summary>
    /// <param name="routerNode">The RouterNode to evaluate.</param>
    /// <param name="context">Current variable context.</param>
    /// <returns>The ID of the node to execute (selected branch's target).</returns>
    /// <exception cref="ArgumentNullException">Thrown when routerNode or context is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when selected branch has no target node.</exception>
    public async Task<Guid> EvaluateRouterAsync(RouterNode routerNode, VariableContextEntity context)
    {
        ArgumentNullException.ThrowIfNull(routerNode);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogEvaluatingRouter(
            routerNode.RouterTask.Name, routerNode.Id, routerNode.RouterTask.Branches.Count);

        // Log selector details for debugging
        var selector = routerNode.RouterTask.Selector;
        var selectorTypeName = selector?.GetType().Name ?? "(null)";
        var selectorExpression = selector?.Expression ?? "(null/empty)";
        _logger.LogRouterSelectorDetails(routerNode.RouterTask.Name, selectorTypeName, selectorExpression);

        // Log branch details
        foreach (var branch in routerNode.RouterTask.Branches)
        {
            var condition = branch.Condition ?? "(null)";
            var isDefault = branch.IsDefaultBranch();
            _logger.LogRouterBranchDetails(
                routerNode.RouterTask.Name, branch.Name, branch.Priority,
                condition, branch.TargetNodeId, isDefault);
        }

        // Log available variables in context
        var variableNames = string.Join(", ", context.GetAllValues().Select(v => $"{v.Key}={v.Value.Value}"));
        _logger.LogRouterVariableContext(routerNode.RouterTask.Name, variableNames);

        // Use BranchSelector to evaluate and select branch
        var selectedBranch = await _branchSelector.SelectBranchAsync(routerNode, context);

        if (selectedBranch.TargetNodeId == null)
        {
            _logger.LogBranchNoTargetNode(routerNode.RouterTask.Name, selectedBranch.Name);
            throw new InvalidOperationException(
                $"Branch '{selectedBranch.Name}' in router '{routerNode.RouterTask.Name}' has no target node. " +
                "Please connect the branch to a target node before executing.");
        }

        _logger.LogBranchSelected(
            routerNode.RouterTask.Name, selectedBranch.Name, selectedBranch.TargetNodeId.Value);

        _logger.LogBranchSelectionInMemory(
            routerNode.RouterTask.Name, selectedBranch.Name, DateTime.UtcNow);

        return selectedBranch.TargetNodeId.Value;
    }
}