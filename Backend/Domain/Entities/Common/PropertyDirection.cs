namespace FHOOE.Freydis.Domain.Entities.Common;

/// <summary>
///     Direction of data flow for a property.
/// </summary>
public enum PropertyDirection
{
    /// <summary>
    ///     TypedProperty receives data as input.
    /// </summary>
    Input,

    /// <summary>
    ///     TypedProperty provides data as output.
    /// </summary>
    Output,

    /// <summary>
    ///     TypedProperty can both receive and provide data.
    /// </summary>
    InputOutput
}