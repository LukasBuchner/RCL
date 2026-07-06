namespace FHOOE.Freydis.Application.Services.Execution.Support.Logging;

/// <summary>
///     Marker class for pipeline event logging. Controls the logger category
///     for the key events of the reactive execution pipeline:
///     inner loop (prerequisite-based triggering, agent start/finish) and
///     outer loop (rescheduling, planned finish updates to agents).
///     <para>
///         Activate by adding this override to appsettings.json Serilog MinimumLevel:
///         <c>"FHOOE.Freydis.Application.Services.Execution.Support.Logging.PipelineEvents": "Verbose"</c>
///     </para>
/// </summary>
public sealed class PipelineEvents;