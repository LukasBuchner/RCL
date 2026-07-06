namespace FHOOE.Freydis.Application.Services.EntityManagement.Procedures.Exceptions;

/// <summary>
///     Exception thrown when a requested procedure is not found in the repository.
/// </summary>
public class ProcedureNotFoundException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the ProcedureNotFoundException.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure that was not found.</param>
    public ProcedureNotFoundException(Guid procedureId)
        : base($"Procedure with ID '{procedureId}' was not found.")
    {
        ProcedureId = procedureId;
    }

    /// <summary>
    ///     Initializes a new instance of the ProcedureNotFoundException with an inner exception.
    /// </summary>
    /// <param name="procedureId">The ID of the procedure that was not found.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public ProcedureNotFoundException(Guid procedureId, Exception innerException)
        : base($"Procedure with ID '{procedureId}' was not found.", innerException)
    {
        ProcedureId = procedureId;
    }

    /// <summary>
    ///     The unique identifier of the procedure that was not found.
    /// </summary>
    public Guid ProcedureId { get; }
}