using FHOOE.Freydis.Application.Services.Common;
using FHOOE.Freydis.Application.Services.Common.Context;
using FHOOE.Freydis.Application.Services.Scheduling.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Loads current entities from repositories and applies pending mutations
///     for scheduling calculations, without waiting for persistence.
/// </summary>
public class CrudDataPreparationService(
    IProcedureRepository procedureRepository,
    IProcedureContext procedureContext,
    ILogger<CrudDataPreparationService> logger)
    : ICrudDataPreparationService
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForCreateAsync<T>(T entity) where T : class
    {
        var (nodes, edges) = await LoadAllEntitiesAsync();
        ApplyMutation(nodes, edges, entity, MutationType.Add);
        return (nodes, edges);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForUpdateAsync<T>(T entity) where T : class
    {
        var (nodes, edges) = await LoadAllEntitiesAsync();
        ApplyMutation(nodes, edges, entity, MutationType.Replace);
        return (nodes, edges);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForDeleteAsync<T>(Guid entityId) where T : class
    {
        var (nodes, edges) = await LoadAllEntitiesAsync();

        if (typeof(T) == typeof(Node)) nodes.RemoveAll(n => n.Id == entityId);
        else if (typeof(T) == typeof(DependencyEdge)) edges.RemoveAll(e => e.Id == entityId);

        return (nodes, edges);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Node> nodes, IReadOnlyList<DependencyEdge> edges)>
        PrepareForTreeDeleteAsync(Guid nodeId)
    {
        var (nodes, edges) = await LoadAllEntitiesAsync();

        var descendantIds = HierarchyTraversal.CollectDescendants(nodeId, nodes).Select(n => n.Id).ToHashSet();
        descendantIds.Add(nodeId);
        nodes.RemoveAll(n => descendantIds.Contains(n.Id));

        return (nodes, edges);
    }

    private async Task<(List<Node> nodes, List<DependencyEdge> edges)> LoadAllEntitiesAsync()
    {
        var procedureId = procedureContext.RequireCurrentProcedureId();

        var nodesTask = procedureRepository.GetNodesByProcedureIdAsync(procedureId);
        var edgesTask = procedureRepository.GetEdgesByProcedureIdAsync(procedureId);

        await Task.WhenAll(nodesTask, edgesTask);

        var nodes = await nodesTask;
        var edges = await edgesTask;

        logger.LogEntitiesLoaded(nodes.Count, edges.Count, procedureId);

        return (nodes, edges);
    }

    private static void ApplyMutation<T>(List<Node> nodes, List<DependencyEdge> edges, T entity,
        MutationType mutation) where T : class
    {
        switch (entity)
        {
            case Node n:
                if (mutation == MutationType.Replace)
                {
                    var idx = nodes.FindIndex(x => x.Id == n.Id);
                    if (idx >= 0) nodes[idx] = n;
                    else nodes.Add(n);
                }
                else
                {
                    nodes.Add(n);
                }

                break;
            case DependencyEdge e:
                if (mutation == MutationType.Replace)
                {
                    var idx = edges.FindIndex(x => x.Id == e.Id);
                    if (idx >= 0) edges[idx] = e;
                    else edges.Add(e);
                }
                else
                {
                    edges.Add(e);
                }

                break;
        }
    }

    private enum MutationType
    {
        Add,
        Replace
    }
}