namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Mode of variable binding operation.
/// </summary>
public enum BindingMode
{
    /// <summary>
    ///     Read value from variable to property (Input binding).
    /// </summary>
    Read,

    /// <summary>
    ///     Write value from property to variable (Output binding).
    /// </summary>
    Write,

    /// <summary>
    ///     Both read and write (InputOutput binding).
    /// </summary>
    ReadWrite
}