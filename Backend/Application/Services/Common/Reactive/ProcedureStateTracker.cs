using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.Common.Reactive;

/// <summary>
///     Unified procedure state tracker that holds all procedure-scoped entity data
///     (nodes, edges, and variables) in a single <see cref="BehaviorSubject{T}" />.
///     Implements <see cref="INodeChangeTracker" />, <see cref="IDependencyEdgeChangeTracker" />,
///     <see cref="IProcedureVariableChangeTracker" />, and <see cref="IProcedureStateScope" />
///     for backward-compatible consumption and lifecycle-driven scoping.
/// </summary>
/// <remarks>
///     <para>
///         All repository queries are scoped to the currently loaded procedure.
///         When no procedure is loaded the subject holds <see cref="ProcedureState.Empty" />,
///         so no cross-procedure data can ever leak into the observable streams.
///     </para>
///     <para>
///         The tracker is driven by <see cref="IProcedureStateScope" /> notifications from
///         <see cref="FHOOE.Freydis.Application.Services.EntityManagement.Procedures.ProcedureOrchestrator" />.
///         A <see cref="OnProcedureLoaded" /> call triggers a scoped repository load; an
///         <see cref="OnProcedureUnloaded" /> call immediately resets the state to
///         <see cref="ProcedureState.Empty" />.
///     </para>
/// </remarks>
public class ProcedureStateTracker
    : INodeChangeTracker, IDependencyEdgeChangeTracker, IProcedureVariableChangeTracker, IProcedureStateScope,
        IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<ProcedureStateTracker> _logger;
    private readonly IProcedureRepository _procedureRepository;

    private readonly BehaviorSubject<ProcedureState> _stateSubject = new(ProcedureState.Empty);

    /// <summary>
    ///     The identifier of the procedure whose data is currently held in <see cref="_stateSubject" />.
    ///     <see langword="null" /> when no procedure is loaded.
    /// </summary>
    private Guid? _loadedProcedureId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcedureStateTracker" /> class.
    ///     The tracker starts in an empty state; call <see cref="OnProcedureLoaded" /> to populate it.
    /// </summary>
    /// <param name="procedureRepository">
    ///     Repository used to load scoped nodes and edges for the active procedure.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ProcedureStateTracker(
        IProcedureRepository procedureRepository,
        ILogger<ProcedureStateTracker> logger)
    {
        _procedureRepository = procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _disposables.Add(_stateSubject);
    }

    /// <summary>
    ///     Indicates whether a procedure has been successfully loaded and state is populated.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///     Gets the unified procedure state observable.
    ///     Emits <see cref="ProcedureState.Empty" /> when no procedure is loaded.
    /// </summary>
    public IObservable<ProcedureState> State => _stateSubject.AsObservable();

    // --- IDependencyEdgeChangeTracker ---

    IObservable<IReadOnlyList<DependencyEdge>> IDependencyEdgeChangeTracker.Edges =>
        _stateSubject.Select(s => s.Edges).DistinctUntilChanged();

    void IDependencyEdgeChangeTracker.UpdateEntities(IReadOnlyList<DependencyEdge> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var scoped = FilterToLoadedProcedure(entities, static e => e.ProcedureId, "DependencyEdge");
        if (scoped is null) return;

        _logger.LogEdgesUpdated(scoped.Count);
        var current = _stateSubject.Value;
        _stateSubject.OnNext(current with { Edges = scoped });
    }

    IReadOnlyList<DependencyEdge> IDependencyEdgeChangeTracker.GetCurrentEdges()
    {
        return _stateSubject.Value.Edges;
    }

    async Task IDependencyEdgeChangeTracker.RefreshFromRepositoryAsync()
    {
        if (_loadedProcedureId is null)
        {
            _logger.LogNoProcedureLoadedForRefresh("DependencyEdge");
            return;
        }

        var id = _loadedProcedureId.Value;
        await RefreshEntitiesAsync(
            () => _procedureRepository.GetEdgesByProcedureIdAsync(id),
            "DependencyEdge",
            edges => _stateSubject.Value with { Edges = edges });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _disposables.Dispose();
    }

    // --- INodeChangeTracker ---

    IObservable<IReadOnlyList<Node>> INodeChangeTracker.Nodes =>
        _stateSubject.Select(s => s.Nodes).DistinctUntilChanged();

    void INodeChangeTracker.UpdateEntities(IReadOnlyList<Node> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var scoped = FilterToLoadedProcedure(entities, static n => n.ProcedureId, "Node");
        if (scoped is null) return;

        _logger.LogNodesUpdated(scoped.Count);
        var current = _stateSubject.Value;
        _stateSubject.OnNext(current with { Nodes = scoped });
    }

    IReadOnlyList<Node> INodeChangeTracker.GetCurrentNodes()
    {
        return _stateSubject.Value.Nodes;
    }

    async Task INodeChangeTracker.RefreshFromRepositoryAsync()
    {
        if (_loadedProcedureId is null)
        {
            _logger.LogNoProcedureLoadedForRefresh("Node");
            return;
        }

        var id = _loadedProcedureId.Value;
        await RefreshEntitiesAsync(
            () => _procedureRepository.GetNodesByProcedureIdAsync(id),
            "Node",
            nodes => _stateSubject.Value with { Nodes = nodes });
    }

    // --- IProcedureVariableChangeTracker ---

    IObservable<IReadOnlyList<VariableDefinition>> IProcedureVariableChangeTracker.Variables =>
        _stateSubject.Select(s => s.Variables).DistinctUntilChanged();

    void IProcedureVariableChangeTracker.NotifyChanged(IReadOnlyList<VariableDefinition> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        _logger.LogProcedureStateVariablesChanged(variables.Count);
        var current = _stateSubject.Value;
        _stateSubject.OnNext(current with { Variables = variables });
    }

    void IProcedureVariableChangeTracker.NotifyUnloaded()
    {
        _logger.LogProcedureVariablesCleared();
        var current = _stateSubject.Value;
        _stateSubject.OnNext(current with { Variables = [] });
    }

    /// <summary>
    ///     Gets the current procedure state snapshot.
    /// </summary>
    /// <returns>
    ///     The current <see cref="ProcedureState" />, containing only data for the loaded procedure.
    ///     Returns <see cref="ProcedureState.Empty" /> when no procedure is loaded.
    /// </returns>
    public ProcedureState GetCurrentState()
    {
        return _stateSubject.Value;
    }

    /// <inheritdoc />
    public void OnProcedureLoaded(Guid procedureId)
    {
        TriggerProcedureLoad(procedureId);
    }

    /// <inheritdoc />
    public void OnProcedureUnloaded()
    {
        TriggerProcedureUnload();
    }

    // --- Private helpers ---

    /// <summary>
    ///     Enforces the single-loaded-procedure invariant at the tracker's public write boundary.
    ///     Rejects the update entirely when no procedure is loaded (and logs at warning level if
    ///     the caller supplied non-empty data).  When a procedure is loaded, returns the incoming
    ///     entities filtered to those whose <see cref="Node.ProcedureId" /> / <see cref="DependencyEdge.ProcedureId" />
    ///     matches the active procedure.  Foreign-procedure entities are dropped and a structured
    ///     warning is emitted so upstream callers that leak cross-procedure data can be located.
    /// </summary>
    /// <typeparam name="T">The entity type being filtered.</typeparam>
    /// <param name="entities">The incoming update payload.</param>
    /// <param name="procedureIdSelector">Projection that extracts the entity's <c>ProcedureId</c>.</param>
    /// <param name="entityTypeName">Human-readable entity name for the structured log.</param>
    /// <returns>
    ///     The list filtered to the currently loaded procedure, or <see langword="null" />
    ///     when the update must be rejected because no procedure is loaded.  The returned
    ///     reference equals <paramref name="entities" /> when no filtering was necessary,
    ///     so the common hot path avoids unnecessary allocation.
    /// </returns>
    private IReadOnlyList<T>? FilterToLoadedProcedure<T>(
        IReadOnlyList<T> entities,
        Func<T, Guid> procedureIdSelector,
        string entityTypeName)
    {
        if (_loadedProcedureId is null)
        {
            if (entities.Count > 0)
                _logger.LogUpdateRejectedNoProcedure(entityTypeName, entities.Count);
            return null;
        }

        var loaded = _loadedProcedureId.Value;
        var droppedCount = entities.Count(t => procedureIdSelector(t) != loaded);

        if (droppedCount == 0)
            return entities;

        _logger.LogCrossProcedureEntitiesDropped(entityTypeName, droppedCount, loaded);

        var scoped = new List<T>(entities.Count - droppedCount);
        scoped.AddRange(entities.Where(t => procedureIdSelector(t) == loaded));

        return scoped;
    }

    /// <summary>
    ///     Fires a background load of nodes and edges for <paramref name="procedureId" />.
    ///     Uses fire-and-forget with a synchronous ContinueWith guard so that any
    ///     unexpected exception escaping the inner try/catch is still logged.
    /// </summary>
    /// <param name="procedureId">The procedure to load data for.</param>
    private void TriggerProcedureLoad(Guid procedureId)
    {
        _loadedProcedureId = procedureId;
        IsInitialized = false;

        _ = LoadForProcedureAsync(procedureId).ContinueWith(
            t => _logger.LogUnhandledInitException(t.Exception!.InnerException!, "ProcedureStateTracker"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    ///     Clears all cached state and emits <see cref="ProcedureState.Empty" /> to all subscribers.
    /// </summary>
    private void TriggerProcedureUnload()
    {
        _loadedProcedureId = null;
        IsInitialized = false;
        _stateSubject.OnNext(ProcedureState.Empty);
        _logger.LogProcedureStateCleared();
    }

    /// <summary>
    ///     Loads nodes and edges from the repository for the specified procedure and
    ///     pushes the resulting <see cref="ProcedureState" /> into the subject.
    ///     Variables are preserved from the current state so that the
    ///     <see cref="IProcedureVariableChangeTracker" /> path remains the authoritative
    ///     writer for variable data.
    /// </summary>
    /// <param name="procedureId">The procedure whose nodes and edges are to be loaded.</param>
    private async Task LoadForProcedureAsync(Guid procedureId)
    {
        try
        {
            var nodesTask = _procedureRepository.GetNodesByProcedureIdAsync(procedureId);
            var edgesTask = _procedureRepository.GetEdgesByProcedureIdAsync(procedureId);
            await Task.WhenAll(nodesTask, edgesTask);

            // Guard: if the procedure was unloaded or switched while we awaited, discard results.
            if (_loadedProcedureId != procedureId)
            {
                _logger.LogStaleProcedureLoadDiscarded(procedureId);
                return;
            }

            var state = new ProcedureState
            {
                Nodes = await nodesTask,
                Edges = await edgesTask,
                Variables = _stateSubject.Value.Variables
            };

            _logger.LogInitialProcedureStateLoaded(state.Nodes.Count, state.Edges.Count);

            _stateSubject.OnNext(state);
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogInitialProcedureStateLoadFailed(ex);
        }
    }

    /// <summary>
    ///     Loads a single entity collection from the repository with retry logic and pushes
    ///     the merged result into <see cref="_stateSubject" />.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being refreshed.</typeparam>
    /// <param name="loadFunc">Factory that issues the scoped repository query.</param>
    /// <param name="entityTypeName">Human-readable name used in log messages.</param>
    /// <param name="stateUpdater">
    ///     Produces the new <see cref="ProcedureState" /> by merging the freshly loaded
    ///     entities into the current state value.
    /// </param>
    private async Task RefreshEntitiesAsync<TEntity>(
        Func<Task<List<TEntity>>> loadFunc,
        string entityTypeName,
        Func<List<TEntity>, ProcedureState> stateUpdater) where TEntity : class
    {
        const int maxRetries = 3;
        var backoffMs = 100;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                var entities = await loadFunc();
                _logger.LogEntitiesRefreshed(entities.Count, entityTypeName, attempt, maxRetries);
                _stateSubject.OnNext(stateUpdater(entities));
                return;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    _logger.LogRefreshTransientFailure(ex, entityTypeName, attempt, maxRetries, backoffMs);
                    await Task.Delay(backoffMs);
                    backoffMs *= 2;
                }
                else
                {
                    _logger.LogRefreshFinalFailure(ex, entityTypeName, maxRetries);
                }
            }
    }
}