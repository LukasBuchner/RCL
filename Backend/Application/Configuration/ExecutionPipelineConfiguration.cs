namespace FHOOE.Freydis.Application.Configuration;

/// <summary>
///     Configuration options for the reactive execution pipeline in <c>ExecutionOrchestrator</c>.
///     Controls sampling intervals for the reschedule computation and the downstream publish rates
///     to agents and the frontend.
/// </summary>
public class ExecutionPipelineConfiguration
{
    /// <summary>
    ///     Sampling interval (in milliseconds) applied to incoming reschedule requests before
    ///     they enter the computation pipeline. Collapses bursts of requests by emitting only the
    ///     latest request per interval. Default is 1 ms.
    /// </summary>
    public double RescheduleSampleIntervalMs { get; set; } = 1.0;

    /// <summary>
    ///     Sampling interval (in milliseconds) for publishing updated planned finish times to
    ///     agents. A lower value means agents receive more frequent schedule updates. Default is 1 ms.
    /// </summary>
    public double AgentPublishIntervalMs { get; set; } = 1.0;

    /// <summary>
    ///     Sampling interval (in milliseconds) for publishing node changes to the frontend.
    ///     A higher value reduces frontend render pressure at the cost of slightly less responsive
    ///     UI updates during execution. Default is 10 ms.
    /// </summary>
    public double FrontendPublishIntervalMs { get; set; } = 10.0;

    /// <summary>
    ///     Sampling interval (in milliseconds) for publishing <see cref="Services.Execution.Monitoring.ExecutionTimingInfo" />
    ///     snapshots. Consumers (frontend UI, adaptive agents) may subscribe at different rates; this
    ///     is the producer-side ceiling, so set it to the fastest interested party's needs — slower
    ///     consumers throttle on their own side. Default is 10 ms.
    /// </summary>
    public double TimingPublishIntervalMs { get; set; } = 10.0;
}