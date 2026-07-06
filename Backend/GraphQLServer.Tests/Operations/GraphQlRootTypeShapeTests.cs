using FHOOE.Freydis.GraphQLServer.Operations;

namespace FHOOE.Freydis.GraphQLServer.Tests.Operations;

/// <summary>
///     Guards the structural contract HotChocolate requires of GraphQL root types: each must be a
///     concrete, instantiable class. HotChocolate constructs an instance of the root type to invoke
///     its instance resolvers (for the subscription root these include <c>NodesChanged</c>,
///     <c>DependencyEdgesChanged</c>, and <c>ProcedureValidationChanged</c>). An abstract root type
///     cannot be instantiated, so every operation on it throws <see cref="NullReferenceException" />
///     at resolve time — a failure that schema construction does not catch and that therefore only
///     surfaces at runtime when a client first issues the operation.
/// </summary>
public class GraphQlRootTypeShapeTests
{
    [Theory]
    [InlineData(typeof(Query))]
    [InlineData(typeof(Mutation))]
    [InlineData(typeof(Subscription))]
    public void GraphQlRootType_IsConcrete_SoHotChocolateCanInvokeInstanceResolvers(Type rootType)
    {
        Assert.False(
            rootType.IsAbstract,
            $"{rootType.Name} must be a non-abstract, instantiable class so HotChocolate can construct it " +
            "and run its instance resolvers; an abstract root type throws NullReferenceException at resolve " +
            "time for every operation issued against it.");
    }
}