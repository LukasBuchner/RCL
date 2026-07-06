using Dapper;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     PostgreSQL repository for PositionTag entities using Dapper.
/// </summary>
public class PositionTagRepository : GenericPostgresRepository<PositionTag>
{
    public PositionTagRepository(PostgresDbContext context, ILogger<PositionTagRepository>? logger = null)
        : base(context, "position_tags", logger)
    {
    }

    public override async Task<List<PositionTag>> GetAllAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, tag, position FROM position_tags";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("position_tags");
            return [];
        }
    }

    public override async Task<PositionTag?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, tag, position FROM position_tags WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("position_tag", id);
            return null;
        }
    }

    public override async Task<List<PositionTag>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "SELECT id, tag, position FROM position_tags WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("position_tags");
            return [];
        }
    }

    public override async Task<PositionTag> CreateAsync(PositionTag entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO position_tags (id, tag, position)
                           VALUES (@Id, @Tag, @Position::jsonb)
                           ON CONFLICT (id) DO UPDATE SET
                               tag = EXCLUDED.tag,
                               position = EXCLUDED.position
                           """;

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Tag,
            Position = SerializeToJson(entity.Position)
        });

        return entity;
    }

    public override async Task<bool> UpdateAsync(PositionTag entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE position_tags SET
                               tag = @Tag,
                               position = @Position::jsonb
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Tag,
            Position = SerializeToJson(entity.Position)
        });

        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM position_tags WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    private PositionTag MapFromRow(dynamic row)
    {
        var pos = DeserializeFromJson<Position>(row.position as string);
        if (pos is null)
            Logger?.LogPositionTagPositionFallback((Guid)row.id);

        return new PositionTag
        {
            Id = row.id,
            Tag = row.tag,
            Position = pos ?? new Position()
        };
    }
}