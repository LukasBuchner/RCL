using FHOOE.Freydis.Domain.Entities.Common;

namespace FHOOE.Freydis.GraphQLServer.Services.Mappers;

/// <summary>
///     Maps TypedValue domain objects to GraphQL property value output types.
/// </summary>
public interface ITypedValueMapper
{
    /// <summary>
    ///     Maps a TypedValue to the appropriate PropertyValue union member.
    /// </summary>
    /// <param name="typedValue">The domain TypedValue to map</param>
    /// <returns>GraphQL property value type (BooleanValue, NumberValue, etc.)</returns>
    /// <exception cref="InvalidOperationException">If TypedValue.Value is null or type is unsupported</exception>
    object MapToPropertyValue(TypedValue typedValue);
}