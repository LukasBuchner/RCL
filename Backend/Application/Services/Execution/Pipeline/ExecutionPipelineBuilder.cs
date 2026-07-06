using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Configuration;
using FHOOE.Freydis.Application.Services.Execution.Rescheduling;
using FHOOE.Freydis.Application.Services.Execution.Support;
using FHOOE.Freydis.Application.Services.Execution.Support.Logging;
using FHOOE.Freydis.Application.Services.Execution.Triggering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Builds the multicast <see cref="ReschedulingResult" /> stream driven by reschedule
///     requests. The pipeline is <c>Sample</c> → <c>ConcurrentMapLatest</c> → <c>Publish</c>;
///     <c>Sample</c> collapses bursts of requests, <c>ConcurrentMapLatest</c> starts
///     reschedule computations concurrently while dropping results from superseded runs,
///     and <c>Publish</c> multicasts the final result stream to all subscribers attached by
///     the caller.
/// </summary>
/// <param name="pipelineOptions">Pipeline sampling configuration supplying the reschedule sample interval.</param>
/// <param name="pipelineLogger">Logger scoped to pipeline events, recording each sampled reschedule reason.</param>
/// <param name="scheduler">
///     The scheduler the reschedule-request <c>Sample</c> runs on; inject a virtual-time
///     scheduler in tests for deterministic sampling.
/// </param>
public sealed class ExecutionPipelineBuilder(
    IOptions<ExecutionPipelineConfiguration> pipelineOptions,
    ILogger<PipelineEvents> pipelineLogger,
    IScheduler scheduler)
    : IExecutionPipelineBuilder
{
    private readonly ILogger<PipelineEvents> _pipeline =
        pipelineLogger ?? throw new ArgumentNullException(nameof(pipelineLogger));

    private readonly ExecutionPipelineConfiguration _pipelineConfig =
        (pipelineOptions ?? throw new ArgumentNullException(nameof(pipelineOptions))).Value;

    private readonly IScheduler _scheduler =
        scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <inheritdoc />
    public IConnectableObservable<ReschedulingResult> Build(
        IObservable<RescheduleReason> rescheduleRequests,
        IExecutionTriggerService executionTriggerService,
        IReschedulingCoordinator reschedulingCoordinator)
    {
        ArgumentNullException.ThrowIfNull(rescheduleRequests);
        ArgumentNullException.ThrowIfNull(executionTriggerService);
        ArgumentNullException.ThrowIfNull(reschedulingCoordinator);

        return rescheduleRequests
            .Sample(TimeSpan.FromMilliseconds(_pipelineConfig.RescheduleSampleIntervalMs), _scheduler)
            .ConcurrentMapLatest(reason => Observable.FromAsync(async ct =>
            {
                _pipeline.LogRescheduleSampled(reason);

                var routerSelections = executionTriggerService.GetRouterSelections();
                reschedulingCoordinator.SetRouterSelections(routerSelections);

                return await reschedulingCoordinator.RescheduleAsync(reason, ct);
            }))
            .Publish();
    }
}