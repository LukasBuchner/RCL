using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace FHOOE.Freydis.Application.Benchmarks.ReactiveExecution;

/// <summary>
///     Custom BenchmarkDotNet summary columns that surface the per-run runtime-conformance result recorded
///     by <see cref="ExecutionConformanceChecker" /> for each (topology, profile, seed) case. The
///     BenchmarkDotNet timing column remains in the table but is incidental; these columns carry the
///     conformance story (the proved invariants held under every pacing uncertainty).
/// </summary>
/// <remarks>
///     Each column resolves the benchmark case's <c>Topology</c>, <c>PacingProfile</c>, and <c>Seed</c>
///     parameters, rebuilds the registry key via <see cref="ExecutionConformanceChecker.BuildKey" />, and
///     reads the recorded <see cref="ConformanceResult" />. A case with no recorded result renders the
///     not-available placeholder.
/// </remarks>
public static class ExecutionConformanceColumns
{
    private const string TopologyParameter = "Topology";
    private const string PacingProfileParameter = "PacingProfile";
    private const string SeedParameter = "Seed";

    /// <summary>
    ///     The full set of conformance columns, in display order, ready to hand to
    ///     <c>ManualConfig.AddColumn</c>.
    /// </summary>
    public static IColumn[] All { get; } =
    [
        new ConformanceColumn(
            "Conformant", "Conformant", 0,
            r => r.Conformant ? "yes" : "no",
            "Run completed and every checked invariant held (no violations)."),
        new ConformanceColumn(
            "OrderingViolations", "OrderViol", 1,
            r => r.OrderingViolations.ToString(CultureInfo.InvariantCulture),
            "Runtime dependency-ordering violations: a node firing before a prerequisite (expected 0)."),
        new ConformanceColumn(
            "RouterOffBranchFirings", "RouterViol", 2,
            r => r.RouterOffBranchFirings.ToString(CultureInfo.InvariantCulture),
            "Off-branch nodes that fired after the router selected another branch (expected 0)."),
        new ConformanceColumn(
            "UnterminatedNodes", "Unterm", 3,
            r => r.UnterminatedNodes.ToString(CultureInfo.InvariantCulture),
            "Reachable nodes that never reached a terminal event (expected 0)."),
        new ConformanceColumn(
            "AdaptiveCouplingHeld", "Coupling", 5,
            r => r.AdaptiveCouplingHeld ? "yes" : "no",
            "Whether every adaptive node finished together with its partner (the robot tracked the human)."),
        new ConformanceColumn(
            "TerminalTransitions", "Terminal", 6,
            r => r.TerminalTransitions.ToString(CultureInfo.InvariantCulture),
            "Nodes that reached a terminal event (Finish or NotSelected): the measure dropping to zero."),
        new ConformanceColumn(
            "RescheduleSnapshots", "Reschedules", 7,
            r => r.RescheduleSnapshots.ToString(CultureInfo.InvariantCulture),
            "Distinct node-change snapshots: outer-loop refinements during the run (context).")
    ];

    /// <summary>
    ///     A single conformance column. It looks up the <see cref="ConformanceResult" /> recorded for the
    ///     benchmark case's parameters and renders one field. Columns live in the
    ///     <see cref="ColumnCategory.Custom" /> category so they sit apart from the built-in statistics.
    /// </summary>
    private sealed class ConformanceColumn : IColumn
    {
        private readonly Func<ConformanceResult, string> _render;

        /// <summary>
        ///     Initializes a conformance column.
        /// </summary>
        /// <param name="id">Stable column identifier.</param>
        /// <param name="columnName">Header shown in the summary table.</param>
        /// <param name="priorityInCategory">Ordering within the custom category.</param>
        /// <param name="render">Renders the field from a result.</param>
        /// <param name="legend">The legend describing the column.</param>
        public ConformanceColumn(
            string id,
            string columnName,
            int priorityInCategory,
            Func<ConformanceResult, string> render,
            string legend)
        {
            Id = id;
            ColumnName = columnName;
            PriorityInCategory = priorityInCategory;
            Legend = legend;
            _render = render;
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string ColumnName { get; }

        /// <inheritdoc />
        public bool AlwaysShow => true;

        /// <inheritdoc />
        public ColumnCategory Category => ColumnCategory.Custom;

        /// <inheritdoc />
        public int PriorityInCategory { get; }

        /// <inheritdoc />
        public bool IsNumeric => false;

        /// <inheritdoc />
        public UnitType UnitType => UnitType.Dimensionless;

        /// <inheritdoc />
        public string Legend { get; }

        /// <inheritdoc />
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            return GetValue(summary, benchmarkCase, summary.Style);
        }

        /// <inheritdoc />
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            if (!TryResolveKey(benchmarkCase, out var key)
                || !ExecutionConformanceChecker.TryGetResult(key, out var result))
                return "?";

            return _render(result);
        }

        /// <inheritdoc />
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
        {
            return false;
        }

        /// <inheritdoc />
        public bool IsAvailable(Summary summary)
        {
            return true;
        }

        /// <summary>
        ///     Reconstructs the checker's registry key from the benchmark case's <c>Topology</c>,
        ///     <c>PacingProfile</c>, and <c>Seed</c> parameters.
        /// </summary>
        /// <param name="benchmarkCase">The case being rendered.</param>
        /// <param name="key">The resolved key when all three parameters are present and well-typed.</param>
        /// <returns><c>true</c> when the key was resolved; otherwise <c>false</c>.</returns>
        private static bool TryResolveKey(BenchmarkCase benchmarkCase, out string key)
        {
            key = string.Empty;

            if (FindParameterValue(benchmarkCase, TopologyParameter) is not Topology topology)
                return false;
            if (FindParameterValue(benchmarkCase, PacingProfileParameter) is not PacingProfile profile)
                return false;
            if (FindParameterValue(benchmarkCase, SeedParameter) is not int seed)
                return false;

            key = ExecutionConformanceChecker.BuildKey(topology, profile, seed);
            return true;
        }

        /// <summary>
        ///     Reads a benchmark-case parameter value by name from the parameter item list, which works for
        ///     <c>[Params]</c> values (unlike <c>ParameterInstances.GetArgument</c>, restricted to arguments).
        /// </summary>
        /// <param name="benchmarkCase">The case being rendered.</param>
        /// <param name="name">The parameter name to read.</param>
        /// <returns>The parameter value, or <c>null</c> when the case has no parameter of that name.</returns>
        private static object? FindParameterValue(BenchmarkCase benchmarkCase, string name)
        {
            return benchmarkCase.Parameters.Items.FirstOrDefault(p => p.Name == name)?.Value;
        }
    }
}