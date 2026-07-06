using Dapper;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     PostgreSQL repository for Skill entities using Dapper.
/// </summary>
public class SkillRepository : GenericPostgresRepository<Skill>
{
    public SkillRepository(PostgresDbContext context, ILogger<SkillRepository>? logger = null)
        : base(context, "skills", logger)
    {
    }

    public override async Task<List<Skill>> GetAllAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, description, properties FROM skills";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("skills");
            return [];
        }
    }

    public override async Task<Skill?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, description, properties FROM skills WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("skill", id);
            return null;
        }
    }

    public override async Task<List<Skill>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, description, properties FROM skills WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("skills");
            return [];
        }
    }

    public override async Task<Skill> CreateAsync(Skill entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO skills (id, name, description, properties)
                           VALUES (@Id, @Name, @Description, @Properties::jsonb)
                           ON CONFLICT (id) DO UPDATE SET
                               name = EXCLUDED.name,
                               description = EXCLUDED.description,
                               properties = EXCLUDED.properties
                           """;

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            Properties = SerializeToJson(entity.Properties)
        });

        return entity;
    }

    public override async Task<bool> UpdateAsync(Skill entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE skills SET
                               name = @Name,
                               description = @Description,
                               properties = @Properties::jsonb
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            Properties = SerializeToJson(entity.Properties)
        });

        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM skills WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    private Skill MapFromRow(dynamic row)
    {
        return new Skill
        {
            Id = row.id,
            Name = row.name,
            Description = row.description,
            Properties = DeserializeFromJson<List<TypedProperty>>(row.properties as string) ?? []
        };
    }
}