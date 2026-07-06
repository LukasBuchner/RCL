namespace FHOOE.Freydis.GraphQLServer.Types;

/// <summary>
///     GraphQL type representing the identity of the currently loaded procedure.
///     Contains only the minimal information needed to identify the procedure,
///     avoiding staleness issues associated with emitting the full procedure object.
/// </summary>
public class LoadedProcedureIdentityDto
{
    /// <summary>
    ///     Sentinel value representing the absence of a loaded procedure.
    ///     Used internally because HotChocolate subscription payloads cannot be null.
    ///     The resolver converts this to a GraphQL null response.
    /// </summary>
    public static readonly LoadedProcedureIdentityDto Unloaded = new();

    /// <summary>
    ///     The unique identifier of the loaded procedure.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The display name of the loaded procedure.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    ///     Indicates whether this DTO represents an actual loaded procedure
    ///     or the <see cref="Unloaded" /> sentinel.
    /// </summary>
    [GraphQLIgnore]
    public bool IsLoaded => Id != Guid.Empty;
}