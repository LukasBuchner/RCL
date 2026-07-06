using System.Reactive.Linq;
using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.Common.Reactive;
using FHOOE.Freydis.Domain.Entities.Procedure;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Application.Services.Execution.Validation;

/// <summary>
///     Singleton reactive tracker that combines node and edge change streams, runs all
///     procedure validators in a single pass, and emits a composite
///     <see cref="ProcedureValidationResult" /> via a <see cref="BehaviorSubject{T}" />.
/// </summary>
/// <remarks>
///     <para>
///         Internally the tracker subscribes once to
///         <c>CombineLatest(<see cref="INodeChangeTracker.Nodes" />, <see cref="IDependencyEdgeChangeTracker.Edges" />)</c>,
///         throttles by one second, and suppresses redundant emissions with
///         <see cref="ValidationResultComparer" />.  This means subscribers receive at most one
///         update per second and only when the validation outcome has actually changed.
///     </para>
///     <para>
///         This tracker is <strong>UX-only and not safety-critical</strong>.  Results may be up
///         to 1 second stale.  The authoritative hard gate is enforced inline by
///         <see cref="IAgentSerializationValidator.Validate" /> inside the
///         <c>ExecutionOrchestrator</c>, independently of this tracker.
///     </para>
///     <para>
///         To add a new validation rule: implement the rule as a new <c>IXxxValidator</c>,
///         inject it via the constructor, call it inside <see cref="RunAllValidators" />, and
///         store its result in a new field on <see cref="ProcedureValidationResult" />.
///         Also extend <see cref="ValidationResultComparer.Equals" /> with the new field.
///     </para>
/// </remarks>
public sealed class ProcedureValidationTracker : IProcedureValidationTracker, IDisposable
{
    private readonly BehaviorSubject<ProcedureValidationResult> _result = new(new ProcedureValidationResult());
    private readonly IDisposable _subscription;

    /// <summary>
    ///     Initializes a new instance of <see cref="ProcedureValidationTracker" /> and starts
    ///     the reactive validation pipeline.
    /// </summary>
    /// <param name="nodeTracker">
    ///     Source of the <see cref="INodeChangeTracker.Nodes" /> observable stream.
    ///     The tracker subscribes to this stream for the lifetime of the instance.
    /// </param>
    /// <param name="edgeTracker">
    ///     Source of the <see cref="IDependencyEdgeChangeTracker.Edges" /> observable stream.
    ///     The tracker subscribes to this stream for the lifetime of the instance.
    /// </param>
    /// <param name="agentSerializationValidator">
    ///     Pure-logic validator that checks whether every agent's skills are connected by a
    ///     Finish-to-Start dependency chain.  Called once per throttled emission.
    /// </param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    public ProcedureValidationTracker(
        INodeChangeTracker nodeTracker,
        IDependencyEdgeChangeTracker edgeTracker,
        IAgentSerializationValidator agentSerializationValidator,
        ILogger<ProcedureValidationTracker> logger)
    {
        _subscription = nodeTracker.Nodes
            .CombineLatest(edgeTracker.Edges,
                (nodes, edges) => RunAllValidators(nodes, edges, agentSerializationValidator))
            .Throttle(TimeSpan.FromSeconds(1))
            .DistinctUntilChanged(ValidationResultComparer.Instance)
            .Subscribe(
                result =>
                {
                    logger.LogProcedureValidationUpdated(result.AgentSerializationViolations.Count);
                    _result.OnNext(result);
                },
                logger.LogProcedureValidationTrackerError);
    }

    /// <inheritdoc />
    public IObservable<ProcedureValidationResult> ValidationResults => _result.AsObservable();

    /// <inheritdoc />
    public ProcedureValidationResult CurrentResult => _result.Value;

    /// <inheritdoc />
    public void Dispose()
    {
        _subscription.Dispose();
        _result.Dispose();
    }

    /// <summary>
    ///     Executes all registered validators against the current graph state and assembles
    ///     the composite <see cref="ProcedureValidationResult" />.
    /// </summary>
    /// <param name="nodes">
    ///     The current snapshot of all procedure nodes, as emitted by
    ///     <see cref="INodeChangeTracker.Nodes" />.
    /// </param>
    /// <param name="edges">
    ///     The current snapshot of all dependency edges, as emitted by
    ///     <see cref="IDependencyEdgeChangeTracker.Edges" />.
    /// </param>
    /// <param name="agentSerializationValidator">
    ///     The agent serialization validator instance passed down from the constructor to avoid
    ///     capturing it in a closure that prevents GC if the subscription outlives the instance.
    /// </param>
    /// <returns>
    ///     A fully-populated <see cref="ProcedureValidationResult" /> containing the output of
    ///     every validator.
    /// </returns>
    private static ProcedureValidationResult RunAllValidators(
        IReadOnlyList<Node> nodes,
        IReadOnlyList<DependencyEdge> edges,
        IAgentSerializationValidator agentSerializationValidator)
    {
        return new ProcedureValidationResult
        {
            AgentSerializationViolations = agentSerializationValidator.Validate(nodes, edges)
        };
    }
}