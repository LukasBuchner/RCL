using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Scheduling.Pipeline;

/// <summary>
///     Handles two-phase notification and persistence for CRUD scheduling results:
///     immediate notification with calculated results, then final with persisted state.
/// </summary>
public interface ICrudNotificationService
{
    /// <summary>
    ///     Observable stream of node changes for real-time notifications.
    /// </summary>
    IObservable<IReadOnlyList<Node>> NodesChanged { get; }

    /// <summary>
    ///     Observable stream of edge changes for real-time notifications.
    /// </summary>
    IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged { get; }

    /// <summary>
    ///     Persists calculated nodes, then notifies subscribers with both calculated and persisted state.
    /// </summary>
    Task PersistAndNotifyAsync(IReadOnlyList<Node> calculatedNodes);

    /// <summary>
    ///     Notifies subscribers with persisted state only (used when scheduling fails).
    /// </summary>
    Task NotifyPersistedStateAsync();
}