using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Application.Services.EntityManagement.Procedures.Exceptions;
using FHOOE.Freydis.Application.Services.EntityManagement.Support.Logging;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures;

/// <summary>
///     Orchestrates procedure loading, unloading, creation, deletion, and state management.
///     Manages which procedure is currently active and coordinates data loading across repositories.
/// </summary>
public class ProcedureOrchestrator : IProcedureOrchestrator, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<ProcedureOrchestrator> _logger;
    private readonly BehaviorSubject<Guid?> _procedureChanges = new(null);
    private readonly IRepository<Procedure> _procedureRepository;
    private readonly IProcedureStateScope _procedureStateScope;
    private readonly IProcedureVariableChangeTracker _procedureVariableChangeTracker;

    private OrchestratorState _state = new(null, null);

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProcedureOrchestrator" /> class.
    /// </summary>
    /// <param name="procedureRepository">Repository for persisting and querying procedure aggregates.</param>
    /// <param name="procedureStateScope">
    ///     Notified when a procedure is loaded or unloaded so that the unified state tracker
    ///     can fetch scoped nodes and edges from the repository.
    /// </param>
    /// <param name="procedureVariableChangeTracker">
    ///     Notified when procedure variables change so that reactive variable streams
    ///     remain synchronized with the loaded procedure.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ProcedureOrchestrator(
        IRepository<Procedure> procedureRepository,
        IProcedureStateScope procedureStateScope,
        IProcedureVariableChangeTracker procedureVariableChangeTracker,
        ILogger<ProcedureOrchestrator> logger)
    {
        _procedureRepository = procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));
        _procedureStateScope = procedureStateScope ?? throw new ArgumentNullException(nameof(procedureStateScope));
        _procedureVariableChangeTracker = procedureVariableChangeTracker ??
                                          throw new ArgumentNullException(nameof(procedureVariableChangeTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Disposes resources used by the orchestrator, including the procedure changes observable.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _procedureChanges?.Dispose();
        _lock.Dispose();
    }

    /// <inheritdoc />
    public async Task<Procedure> LoadProcedureAsync(Guid procedureId)
    {
        _logger.LogProcedureLoadStart(procedureId);

        Procedure procedure;

        await _lock.WaitAsync();
        try
        {
            // If same procedure is already loaded, return it (idempotent)
            if (_state.ProcedureId == procedureId)
            {
                _logger.LogProcedureAlreadyLoaded(procedureId);
                var existing = await _procedureRepository.GetByIdAsync(procedureId)
                               ?? throw new ProcedureNotFoundException(procedureId);
                return existing;
            }

            // Fetch inside the lock to prevent TOCTOU races (e.g. concurrent delete)
            procedure = await _procedureRepository.GetByIdAsync(procedureId)
                        ?? throw new ProcedureNotFoundException(procedureId);

            // Unload current procedure if one is loaded
            if (_state.ProcedureId.HasValue)
            {
                _logger.LogProcedureSwitch(_state.ProcedureId.Value, procedureId);

                var currentProcedure = await _procedureRepository.GetByIdAsync(_state.ProcedureId.Value);
                if (currentProcedure != null)
                {
                    currentProcedure = currentProcedure with { IsLoaded = false };
                    await _procedureRepository.UpdateAsync(currentProcedure);
                }
            }

            // Mark as loaded
            procedure = procedure with
            {
                IsLoaded = true,
                LastLoadedUtc = DateTime.UtcNow
            };

            // Update in repository
            await _procedureRepository.UpdateAsync(procedure);

            // Update in-memory state atomically
            Interlocked.Exchange(ref _state, new OrchestratorState(procedureId, procedure.Name));

            // Emit procedure change notification
            _procedureChanges.OnNext(procedureId);
        }
        finally
        {
            _lock.Release();
        }

        _procedureStateScope.OnProcedureLoaded(procedureId);
        _procedureVariableChangeTracker.NotifyChanged(procedure.Variables);

        _logger.LogProcedureLoadSuccess(procedureId);
        return procedure;
    }

    /// <inheritdoc />
    public async Task UnloadCurrentProcedureAsync()
    {
        Guid? procedureIdToUnload;

        await _lock.WaitAsync();
        try
        {
            procedureIdToUnload = _state.ProcedureId;
            if (!procedureIdToUnload.HasValue)
            {
                _logger.LogNoProcedureLoaded();
                return;
            }

            // Clear in-memory state atomically
            Interlocked.Exchange(ref _state, new OrchestratorState(null, null));

            // Emit procedure change notification (null = no procedure loaded)
            _procedureChanges.OnNext(null);
        }
        finally
        {
            _lock.Release();
        }

        _procedureStateScope.OnProcedureUnloaded();
        _procedureVariableChangeTracker.NotifyUnloaded();

        _logger.LogProcedureUnloadStart(procedureIdToUnload.Value);

        // Update procedure in repository
        var procedure = await _procedureRepository.GetByIdAsync(procedureIdToUnload.Value);
        if (procedure != null)
        {
            procedure = procedure with { IsLoaded = false };
            await _procedureRepository.UpdateAsync(procedure);
        }

        _logger.LogProcedureUnloadSuccess(procedureIdToUnload.Value);
    }

    /// <inheritdoc />
    public async Task<Procedure?> GetLoadedProcedureAsync()
    {
        // Capture a consistent snapshot — reference read is atomic
        var state = _state;
        if (!state.ProcedureId.HasValue) return null;

        return await _procedureRepository.GetByIdAsync(state.ProcedureId.Value);
    }

    /// <inheritdoc />
    public async Task<Procedure> CreateProcedureAsync(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Procedure name cannot be null or empty", nameof(name));

        _logger.LogProcedureCreateStart(name);

        var now = DateTime.UtcNow;
        var procedure = new Procedure
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now,
            RootNodeIds = Array.Empty<Guid>(),
            IsLoaded = false,
            LastLoadedUtc = null
        };

        var createdProcedure = await _procedureRepository.CreateAsync(procedure);

        _logger.LogProcedureCreateSuccess(createdProcedure.Id, name);

        return createdProcedure;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProcedureAsync(Guid procedureId)
    {
        _logger.LogProcedureDeleteStart(procedureId);

        bool deleted;
        var wasLoaded = false;

        await _lock.WaitAsync();
        try
        {
            // Check existence inside the lock to prevent TOCTOU races
            var procedure = await _procedureRepository.GetByIdAsync(procedureId);
            if (procedure == null)
            {
                _logger.LogProcedureNotFound(procedureId);
                return false;
            }

            // If procedure is currently loaded, unload it first
            if (_state.ProcedureId == procedureId)
            {
                _logger.LogProcedureUnloadBeforeDelete(procedureId);
                Interlocked.Exchange(ref _state, new OrchestratorState(null, null));
                _procedureChanges.OnNext(null);
                wasLoaded = true;
            }

            // Delete inside the lock to prevent concurrent re-load
            _logger.LogProcedureCascadeDelete(procedureId);
            deleted = await _procedureRepository.DeleteAsync(procedureId);

            if (deleted)
                _logger.LogProcedureDeleteSuccess(procedureId);
            else
                _logger.LogProcedureDeleteFailed(procedureId);
        }
        finally
        {
            _lock.Release();
        }

        if (wasLoaded)
        {
            _procedureStateScope.OnProcedureUnloaded();
            _procedureVariableChangeTracker.NotifyUnloaded();
        }

        return deleted;
    }

    /// <inheritdoc />
    public Guid? GetLoadedProcedureId()
    {
        return _state.ProcedureId;
    }

    /// <inheritdoc />
    public string? GetLoadedProcedureName()
    {
        return _state.ProcedureName;
    }

    /// <inheritdoc />
    public IObservable<Guid?> ProcedureChanges => _procedureChanges;

    /// <summary>
    ///     Immutable snapshot of the orchestrator's loaded-procedure state.
    ///     Swapped atomically via <see cref="Interlocked.Exchange{T}" /> so that lock-free
    ///     readers (<see cref="GetLoadedProcedureId" />, <see cref="GetLoadedProcedureName" />)
    ///     never observe a torn or inconsistent value. Reference reads are atomic on .NET.
    /// </summary>
    private sealed record OrchestratorState(Guid? ProcedureId, string? ProcedureName);
}