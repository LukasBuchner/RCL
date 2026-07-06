using FHOOE.Freydis.Domain.Entities.Procedure;

namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for RouterTask operations.
///     Represents router task data in a format suitable for application layer processing.
/// </summary>
/// <param name="Name">The name of the router task.</param>
/// <param name="Description">The description of the router task.</param>
/// <param name="StartTime">The start time of the router task.</param>
/// <param name="Duration">The duration of the router task.</param>
/// <param name="FinishTime">The finish time of the router task.</param>
/// <param name="IsExecuting">Whether the router task is currently executing.</param>
/// <param name="Progress">The progress of the router task execution.</param>
/// <param name="Selector">Selector expression for routing.</param>
/// <param name="Branches">Conditional branches for routing.</param>
/// <param name="SelectedBranchTargetNodeId">ID of the target node for the selected branch during execution.</param>
/// <param name="SelectedBranchName">Name of the branch that was selected during execution.</param>
/// <param name="SelectedAtUtc">UTC timestamp when the branch was selected during execution.</param>
/// <param name="ManuallySelectedBranch">Name of the branch manually selected by the user in design mode.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record RouterTaskDto(
    string Name,
    string? Description,
    double StartTime,
    double Duration,
    double? FinishTime,
    bool? IsExecuting,
    double? Progress,
    SelectorExpression Selector,
    IReadOnlyList<ConditionalBranch> Branches,
    Guid? SelectedBranchTargetNodeId,
    string? SelectedBranchName,
    DateTime? SelectedAtUtc,
    string? ManuallySelectedBranch);