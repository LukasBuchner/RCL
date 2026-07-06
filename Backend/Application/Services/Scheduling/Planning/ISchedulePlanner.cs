using FHOOE.Freydis.Scheduling.Core;

namespace FHOOE.Freydis.Application.Services.Scheduling.Planning;

/// <summary>
///     Responsible for planning the schedule of an execution graph.
/// </summary>
public interface ISchedulePlanner
{
    /// <summary>
    ///     Plans execution timing for skills in the given graph.
    /// </summary>
    /// <param name="graph">The execution graph.</param>
    /// <param name="currentTime">
    ///     The current time from which to schedule forward. Tasks not yet started cannot begin before
    ///     this time.
    /// </param>
    /// <returns>True if planning succeeded; otherwise false.</returns>
    bool Plan(IExecutionGraph graph, double currentTime = 0);
}