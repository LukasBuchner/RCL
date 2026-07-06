namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Orchestrates the execution of the loaded procedure from repository data.
/// </summary>
public interface IExecutionOrchestrator
{
    /// <summary>
    ///     Loads the current procedure from repositories, builds an execution graph, schedules it,
    ///     and starts executing the skills. Returns once execution has started; the run itself
    ///     continues on a background task. Progress, completion, and run-time errors surface through
    ///     the node/edge change, timing, and advisory subscriptions rather than this method's result.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels initialization and, once started, the detached execution run.</param>
    /// <returns>True if execution started; false if initialization failed.</returns>
    Task<bool> StartLoadedProcedureAsync(CancellationToken cancellationToken = default);
}