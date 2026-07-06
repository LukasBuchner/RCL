using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.GraphQLServer.Services.Mappers;
using FHOOE.Freydis.GraphQLServer.Support.Logging;

namespace FHOOE.Freydis.GraphQLServer.Types.Resolvers;

/// <summary>
///     Contains field resolvers for the TypedProperty type.
///     Provides custom resolution for the value field to map TypedValue to PropertyValue union.
/// </summary>
public class PropertyResolvers
{
    /// <summary>
    ///     Resolves the value field for a TypedProperty by mapping TypedValue to appropriate PropertyValue type.
    ///     This enables GraphQL to correctly expose both type descriptor and actual value.
    /// </summary>
    public object GetValue(
        [Parent] TypedProperty typedProperty,
        [Service] ITypedValueMapper mapper,
        ILogger<PropertyResolvers> logger)
    {
        try
        {
            return mapper.MapToPropertyValue(typedProperty.Value);
        }
        catch (Exception ex)
        {
            logger.LogPropertyValueMappingFailed(ex, typedProperty.Name);
            throw new InvalidOperationException(
                $"Unable to resolve value for typedProperty '{typedProperty.Name}'", ex);
        }
    }
}