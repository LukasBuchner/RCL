using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Handles cascade deletion of nodes and their referencing edges.
///     Deletes edges first to maintain referential integrity.
/// </summary>
public class CascadeDeletionService(
    IProcedureRepository procedureRepository,
    IProcedureContext procedureContext,
    ILogger<CascadeDeletionService> logger)
    : ICascadeDeletionService
{
    /// <inheritdoc />
    public async Task<bool> DeleteSingleNodeAsync(Guid nodeId)
    {
        logger.LogSingleNodeDeletionStarted(nodeId);

        var procedureId = procedureContext.RequireCurrentProcedureId();
        var allEdges = await procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

        // Find and delete all edges that reference this node
        var orphanedEdges = allEdges.Where(e => e.SourceId == nodeId || e.TargetId == nodeId).ToList();
        await DeleteEdgesAsync(orphanedEdges, nodeId);

        var nodeDeleted = await procedureRepository.DeleteNodeAsync(nodeId);
        if (!nodeDeleted)
        {
            logger.LogNodeDeletionFailed(nodeId);
            return false;
        }

        logger.LogNodeDeletionCompleted(nodeId, orphanedEdges.Count);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteNodeTreeAsync(Guid nodeId)
    {
        logger.LogNodeTreeDeletionStarted(nodeId);

        var procedureId = procedureContext.RequireCurrentProcedureId();

        var allNodes = await procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        var allEdges = await procedureRepository.GetEdgesByProcedureIdAsync(procedureId);
        var descendantNodes = HierarchyTraversal.CollectDescendants(nodeId, allNodes);

        // Collect all node IDs that will be deleted (root + all descendants)
        var nodeIdsToDelete = new List<Guid> { nodeId };
        nodeIdsToDelete.AddRange(descendantNodes.Select(c => c.Id));

        // Find and delete all edges that reference any of the nodes being deleted
        var nodeIdSet = nodeIdsToDelete.ToHashSet();
        var orphanedEdges = allEdges.Where(e => nodeIdSet.Contains(e.SourceId) || nodeIdSet.Contains(e.TargetId))
            .ToList();
        await DeleteEdgesAsync(orphanedEdges, nodeId);

        // Delete all descendant nodes first (leaves before parents)
        foreach (var descendant in descendantNodes) await procedureRepository.DeleteNodeAsync(descendant.Id);

        // Delete the root node
        var parentDeleted = await procedureRepository.DeleteNodeAsync(nodeId);
        if (!parentDeleted)
        {
            logger.LogParentNodeDeletionFailed(nodeId);
            return false;
        }

        logger.LogNodeTreeDeletionCompleted(nodeId, descendantNodes.Count, orphanedEdges.Count);
        return true;
    }

    private async Task DeleteEdgesAsync(List<DependencyEdge> edges, Guid contextNodeId)
    {
        foreach (var edge in edges)
        {
            var deleted = await procedureRepository.DeleteEdgeAsync(edge.Id);
            if (!deleted)
                logger.LogOrphanedEdgeDeletionFailed(edge.Id, contextNodeId);
        }
    }
}