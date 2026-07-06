using Dapper;
using FHOOE.Freydis.Domain.Entities.Common;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Logging;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     PostgreSQL repository for Agent entities using Dapper.
/// </summary>
public class AgentRepository : GenericPostgresRepository<Agent>
{
    public AgentRepository(PostgresDbContext context, ILogger<AgentRepository>? logger = null)
        : base(context, "agents", logger)
    {
    }

    public override async Task<List<Agent>> GetAllAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = """
                               SELECT id, name, skill_ids, representative_color, state, last_seen_utc, metadata
                               FROM agents
                               """;

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("agents");
            return [];
        }
    }

    public override async Task<Agent?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = """
                               SELECT id, name, skill_ids, representative_color, state, last_seen_utc, metadata
                               FROM agents
                               WHERE id = @Id
                               """;

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("agent", id);
            return null;
        }
    }

    public override async Task<List<Agent>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = """
                               SELECT id, name, skill_ids, representative_color, state, last_seen_utc, metadata
                               FROM agents
                               WHERE id = ANY(@Ids)
                               """;

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("agents");
            return [];
        }
    }

    public override async Task<Agent> CreateAsync(Agent entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO agents (id, name, skill_ids, representative_color, state, last_seen_utc, metadata)
                           VALUES (@Id, @Name, @SkillIds, @RepresentativeColor, @State, @LastSeenUtc, @Metadata::jsonb)
                           ON CONFLICT (id) DO UPDATE SET
                               name = EXCLUDED.name,
                               skill_ids = EXCLUDED.skill_ids,
                               representative_color = EXCLUDED.representative_color,
                               state = EXCLUDED.state,
                               last_seen_utc = EXCLUDED.last_seen_utc,
                               metadata = EXCLUDED.metadata
                           """;

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            SkillIds = entity.SkillIds.ToArray(),
            entity.RepresentativeColor,
            State = entity.State.ToString(),
            entity.LastSeenUtc,
            Metadata = SerializeToJson(entity.Metadata)
        });

        return entity;
    }

    public override async Task<bool> UpdateAsync(Agent entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE agents SET
                               name = @Name,
                               skill_ids = @SkillIds,
                               representative_color = @RepresentativeColor,
                               state = @State,
                               last_seen_utc = @LastSeenUtc,
                               metadata = @Metadata::jsonb
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            SkillIds = entity.SkillIds.ToArray(),
            entity.RepresentativeColor,
            State = entity.State.ToString(),
            entity.LastSeenUtc,
            Metadata = SerializeToJson(entity.Metadata)
        });

        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM agents WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    private Agent MapFromRow(dynamic row)
    {
        var skillIds = row.skill_ids is Guid[] guids
            ? guids.ToList()
            : [];

        if (row.skill_ids is not Guid[])
            Logger?.LogAgentSkillIdsFallback((Guid)row.id);

        var state = Enum.TryParse<AgentState>((string)row.state, out var parsedState)
            ? parsedState
            : AgentState.Registered;

        if (!Enum.TryParse<AgentState>((string)row.state, out _))
            Logger?.LogAgentStateParseFallback((Guid)row.id, (string)row.state, AgentState.Registered);

        return new Agent
        {
            Id = row.id,
            Name = row.name,
            SkillIds = skillIds,
            RepresentativeColor = row.representative_color,
            State = state,
            LastSeenUtc = row.last_seen_utc,
            Metadata = DeserializeFromJson<Dictionary<string, object>>(row.metadata as string)
        };
    }
}