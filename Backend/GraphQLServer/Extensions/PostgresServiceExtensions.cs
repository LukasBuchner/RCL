using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

namespace FHOOE.Freydis.GraphQLServer.Extensions;

/// <summary>
///     Extension methods for configuring PostgreSQL services.
/// </summary>
public static class PostgresServiceExtensions
{
    /// <summary>
    ///     Adds PostgreSQL persistence services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgresPersistence(
        this IServiceCollection services)
    {
        // Register PostgreSQL context as Singleton
        services.AddSingleton<PostgresDbContext>();

        // Register repositories as Singleton to support singleton application services with reactive subscriptions
        services.AddSingleton<IRepository<Agent>, AgentRepository>();
        services.AddSingleton<IRepository<Skill>, SkillRepository>();
        services.AddSingleton<IRepository<SceneObject>, SceneObjectRepository>();
        services.AddSingleton<IRepository<PositionTag>, PositionTagRepository>();

        // Register ProcedureRepository as the aggregate root repository for procedures, nodes, and edges
        services.AddSingleton<ProcedureRepository>();
        services.AddSingleton<IProcedureRepository>(sp => sp.GetRequiredService<ProcedureRepository>());
        services.AddSingleton<IRepository<Procedure>>(sp => sp.GetRequiredService<ProcedureRepository>());

        // Adapt the aggregate IProcedureRepository to generic IRepository<T> for Node and DependencyEdge
        // so the repository-caching layer can resolve and cache them.
        services.AddSingleton<IRepository<Node>, NodeRepositoryAdapter>();
        services.AddSingleton<IRepository<DependencyEdge>, DependencyEdgeRepositoryAdapter>();

        return services;
    }
}