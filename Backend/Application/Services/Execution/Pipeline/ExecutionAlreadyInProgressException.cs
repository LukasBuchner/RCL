using FHOOE.Freydis.Application.Services.Execution.Validation;

namespace FHOOE.Freydis.Application.Services.Execution.Pipeline;

/// <summary>
///     Thrown when a procedure execution is requested while another execution is already in progress.
///     The orchestrator is a singleton and supports only one concurrent execution at a time.
/// </summary>
public sealed class ExecutionAlreadyInProgressException : ExecutionPreConditionException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ExecutionAlreadyInProgressException" /> class
    ///     with a default message.
    /// </summary>
    public ExecutionAlreadyInProgressException()
        : base(
            "An execution is already in progress. Wait for the current execution to complete before starting a new one.")
    {
    }

    /// <inheritdoc />
    public override string ErrorCode => "EXECUTION_ALREADY_IN_PROGRESS";
}