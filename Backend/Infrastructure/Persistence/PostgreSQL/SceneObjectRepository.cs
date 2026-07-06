using Dapper;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     PostgreSQL repository for SceneObject entities using Dapper.
/// </summary>
public class SceneObjectRepository : GenericPostgresRepository<SceneObject>
{
    public SceneObjectRepository(PostgresDbContext context, ILogger<SceneObjectRepository>? logger = null)
        : base(context, "scene_objects", logger)
    {
    }

    public override async Task<List<SceneObject>> GetAllAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, position FROM scene_objects";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("scene_objects");
            return [];
        }
    }

    public override async Task<SceneObject?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, position FROM scene_objects WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("scene_object", id);
            return null;
        }
    }

    public override async Task<List<SceneObject>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, name, position FROM scene_objects WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("scene_objects");
            return [];
        }
    }

    public override async Task<SceneObject> CreateAsync(SceneObject entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO scene_objects (id, name, position)
                           VALUES (@Id, @Name, @Position::jsonb)
                           ON CONFLICT (id) DO UPDATE SET
                               name = EXCLUDED.name,
                               position = EXCLUDED.position
                           """;

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            Position = SerializeToJson(entity.Position)
        });

        return entity;
    }

    public override async Task<bool> UpdateAsync(SceneObject entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE scene_objects SET
                               name = @Name,
                               position = @Position::jsonb
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            Position = SerializeToJson(entity.Position)
        });

        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM scene_objects WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    private SceneObject MapFromRow(dynamic row)
    {
        var pos = DeserializeFromJson<Position>(row.position as string);
        if (pos is null)
            Logger?.LogSceneObjectPositionFallback((Guid)row.id);

        return new SceneObject
        {
            Id = row.id,
            Name = row.name,
            Position = pos ?? new Position()
        };
    }
}