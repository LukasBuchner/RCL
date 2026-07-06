namespace FHOOE.Freydis.Domain.Entities.Variables;

/// <summary>
///     Scope level where a variable is accessible.
/// </summary>
public enum VariableScope
{
    Procedure, // Available within procedure
    Task, // Available within task
    Global // Available across procedures
}