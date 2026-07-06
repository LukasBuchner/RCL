namespace FHOOE.Freydis.GraphQLServer.Types.DTOs;

/// <summary>
///     Data Transfer Object for Task operations.
///     Represents task data in a format suitable for application layer processing.
/// </summary>
/// <param name="Name">The name of the task.</param>
/// <param name="Description">The description of the task.</param>
/// <param name="StartTime">The start time of the task.</param>
/// <param name="Duration">The duration of the task.</param>
/// <param name="FinishTime">The finish time of the task.</param>
/// <param name="IsExecuting">Whether the task is currently executing.</param>
/// <param name="Progress">The progress of the task execution.</param>
/// <remarks>
///     This DTO separates the application layer from GraphQL-specific input types,
///     maintaining Clean Architecture principles.
/// </remarks>
public record TaskDto(
    string Name,
    string? Description,
    double StartTime,
    double Duration,
    double? FinishTime,
    bool? IsExecuting,
    double? Progress);