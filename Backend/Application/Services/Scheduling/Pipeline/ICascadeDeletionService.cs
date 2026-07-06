namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Performs cascade deletion of nodes and their referencing edges,
///     supporting both single-node and tree deletion patterns.
/// </summary>
public interface ICascadeDeletionService
{
    /// <summary>
    ///     Deletes a single node and all edges that reference it.
    /// </summary>
    Task<bool> DeleteSingleNodeAsync(Guid nodeId);

    /// <summary>
    ///     Deletes a node tree (parent + all descendants) and all referencing edges.
    /// </summary>
    Task<bool> DeleteNodeTreeAsync(Guid nodeId);
}