using FHOOE.Freydis.Domain.Entities.Procedure;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Service responsible for publishing execution-related events to the frontend.
///     Manages change trackers and observables for real-time updates during procedure execution.
/// </summary>
public interface IExecutionEventPublisher
{
    /// <summary>
    ///     Gets the observable stream for real-time node changes during execution.
    /// </summary>
    IObservable<IReadOnlyList<Node>> NodesChanged { get; }

    /// <summary>
    ///     Gets the observable stream for real-time edge changes during execution.
    /// </summary>
    IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged { get; }

    /// <summary>
    ///     <see cref="IObserver{T}" /> surface that forwards <c>OnNext</c> to
    ///     <see cref="PublishNodeChanges" /> and logs-then-swallows <c>OnError</c>. Its
    ///     <c>OnCompleted</c> is a deliberate no-op so that a per-execution source observable
    ///     completing does not tear down the singleton <see cref="NodesChanged" /> channel.
    ///     Consumers subscribed before the current execution continue to receive values from
    ///     subsequent executions without re-subscribing.
    /// </summary>
    IObserver<IReadOnlyList<Node>> NodesObserver { get; }

    /// <summary>
    ///     Publishes node changes to all subscribers.
    /// </summary>
    /// <param name="updatedNodes">The updated list of nodes to publish.</param>
    void PublishNodeChanges(IReadOnlyList<Node> updatedNodes);

    /// <summary>
    ///     Publishes edge changes to all subscribers.
    /// </summary>
    /// <param name="updatedEdges">The updated list of edges to publish.</param>
    void PublishEdgeChanges(IReadOnlyList<DependencyEdge> updatedEdges);

    /// <summary>
    ///     Refreshes both node and edge change trackers from their respective repositories,
    ///     resetting in-memory state to match persisted data.
    /// </summary>
    Task RefreshChangeTrackersFromRepositoryAsync();
}