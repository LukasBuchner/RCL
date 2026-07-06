using System.Reactive;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Execution.Events;

/// <summary>
///     Service responsible for publishing execution-related events to the frontend.
///     Manages change trackers and observables for real-time updates during procedure execution.
/// </summary>
public class ExecutionEventPublisher : IExecutionEventPublisher
{
    private readonly IDependencyEdgeChangeTracker _edgeChangeTracker;
    private readonly INodeChangeTracker _nodeChangeTracker;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionEventPublisher" /> class.
    /// </summary>
    /// <param name="nodeChangeTracker">The node change tracker for observing node updates.</param>
    /// <param name="edgeChangeTracker">The edge change tracker for observing edge updates.</param>
    /// <param name="logger">Logger for publish failures surfaced through the observer surface.</param>
    public ExecutionEventPublisher(
        INodeChangeTracker nodeChangeTracker,
        IDependencyEdgeChangeTracker edgeChangeTracker,
        ILogger<ExecutionEventPublisher> logger)
    {
        _nodeChangeTracker = nodeChangeTracker ?? throw new ArgumentNullException(nameof(nodeChangeTracker));
        _edgeChangeTracker = edgeChangeTracker ?? throw new ArgumentNullException(nameof(edgeChangeTracker));
        var logger1 = logger ?? throw new ArgumentNullException(nameof(logger));

        NodesObserver = Observer.Create<IReadOnlyList<Node>>(
            PublishNodeChanges,
            ex => logger1.LogNodePublishFailed(ex),
            () =>
            {
                /* singleton channel stays hot across executions */
            });
    }

    /// <inheritdoc />
    public IObservable<IReadOnlyList<Node>> NodesChanged => _nodeChangeTracker.Nodes;

    /// <inheritdoc />
    public IObservable<IReadOnlyList<DependencyEdge>> EdgesChanged => _edgeChangeTracker.Edges;

    /// <inheritdoc />
    public IObserver<IReadOnlyList<Node>> NodesObserver { get; }

    /// <inheritdoc />
    public void PublishNodeChanges(IReadOnlyList<Node> updatedNodes)
    {
        ArgumentNullException.ThrowIfNull(updatedNodes, nameof(updatedNodes));
        _nodeChangeTracker.UpdateEntities(updatedNodes);
    }

    /// <inheritdoc />
    public void PublishEdgeChanges(IReadOnlyList<DependencyEdge> updatedEdges)
    {
        ArgumentNullException.ThrowIfNull(updatedEdges, nameof(updatedEdges));
        _edgeChangeTracker.UpdateEntities(updatedEdges);
    }

    /// <inheritdoc />
    public async Task RefreshChangeTrackersFromRepositoryAsync()
    {
        await _nodeChangeTracker.RefreshFromRepositoryAsync();
        await _edgeChangeTracker.RefreshFromRepositoryAsync();
    }
}