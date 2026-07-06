using Dapper;
using FHOOE.Freydis.Domain;
using FHOOE.Freydis.Domain.Entities.Procedure;
using FHOOE.Freydis.Domain.Entities.Variables;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Logging;
using Task = FHOOE.Freydis.Domain.Entities.Procedure.Task;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     PostgreSQL repository for the Procedure aggregate root using Dapper.
///     Manages procedures, nodes, and dependency edges as a single aggregate.
/// </summary>
public class ProcedureRepository : GenericPostgresRepository<Procedure>, IProcedureRepository
{
    // ========================================================================
    // Procedure operations
    // ========================================================================

    private const string ProcedureSelectColumns = """
                                                  id, name, description, created_at_utc, last_updated_at_utc,
                                                  root_node_ids, variables, is_loaded, last_loaded_utc
                                                  """;

    // ========================================================================
    // Node operations
    // ========================================================================

    private const string NodeSelectColumns = """
                                             id, procedure_id, node_type, position, parent_id,
                                             extent, width, height, selectable, selected,
                                             draggable, dragging, hidden, data
                                             """;

    // ========================================================================
    // Edge operations
    // ========================================================================

    private const string EdgeSelectColumns = "id, procedure_id, source_id, target_id, source_handle, target_handle";

    public ProcedureRepository(PostgresDbContext context, ILogger<ProcedureRepository>? logger = null)
        : base(context, "procedures", logger)
    {
    }

    public override async Task<List<Procedure>> GetAllAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {ProcedureSelectColumns} FROM procedures";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapProcedureFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("procedures");
            return [];
        }
    }

    public override async Task<Procedure?> GetByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {ProcedureSelectColumns} FROM procedures WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapProcedureFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("procedure", id);
            return null;
        }
    }

    public override async Task<List<Procedure>> GetByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {ProcedureSelectColumns} FROM procedures WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapProcedureFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("procedures");
            return [];
        }
    }

    public override async Task<Procedure> CreateAsync(Procedure entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO procedures (id, name, description, created_at_utc, last_updated_at_utc,
                               root_node_ids, variables, is_loaded, last_loaded_utc)
                           VALUES (@Id, @Name, @Description, @CreatedAtUtc, @LastUpdatedAtUtc,
                               @RootNodeIds, @Variables::jsonb, @IsLoaded, @LastLoadedUtc)
                           ON CONFLICT (id) DO UPDATE SET
                               name = EXCLUDED.name,
                               description = EXCLUDED.description,
                               created_at_utc = EXCLUDED.created_at_utc,
                               last_updated_at_utc = EXCLUDED.last_updated_at_utc,
                               root_node_ids = EXCLUDED.root_node_ids,
                               variables = EXCLUDED.variables,
                               is_loaded = EXCLUDED.is_loaded,
                               last_loaded_utc = EXCLUDED.last_loaded_utc
                           """;

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            entity.CreatedAtUtc,
            entity.LastUpdatedAtUtc,
            RootNodeIds = entity.RootNodeIds.ToArray(),
            Variables = SerializeToJson(entity.Variables),
            entity.IsLoaded,
            entity.LastLoadedUtc
        });

        return entity;
    }

    public override async Task<bool> UpdateAsync(Procedure entity)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE procedures SET
                               name = @Name,
                               description = @Description,
                               created_at_utc = @CreatedAtUtc,
                               last_updated_at_utc = @LastUpdatedAtUtc,
                               root_node_ids = @RootNodeIds,
                               variables = @Variables::jsonb,
                               is_loaded = @IsLoaded,
                               last_loaded_utc = @LastLoadedUtc
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            entity.CreatedAtUtc,
            entity.LastUpdatedAtUtc,
            RootNodeIds = entity.RootNodeIds.ToArray(),
            Variables = SerializeToJson(entity.Variables),
            entity.IsLoaded,
            entity.LastLoadedUtc
        });

        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM procedures WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<List<Node>> GetAllNodesAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {NodeSelectColumns} FROM nodes";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapNodeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("nodes");
            return [];
        }
    }

    public async Task<List<Node>> GetNodesByProcedureIdAsync(Guid procedureId)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {NodeSelectColumns} FROM nodes WHERE procedure_id = @ProcedureId";

            var rows = await connection.QueryAsync(sql, new { ProcedureId = procedureId });
            return rows.Select(MapNodeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("nodes");
            return [];
        }
    }

    public async Task<Node?> GetNodeByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {NodeSelectColumns} FROM nodes WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapNodeFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("node", id);
            return null;
        }
    }

    public async Task<List<Node>> GetNodesByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {NodeSelectColumns} FROM nodes WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapNodeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("nodes");
            return [];
        }
    }

    public async Task<Node> CreateNodeAsync(Node node)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO nodes (id, procedure_id, node_type, position, parent_id,
                               extent, width, height, selectable, selected,
                               draggable, dragging, hidden, data)
                           VALUES (@Id, @ProcedureId, @NodeType, @Position::jsonb, @ParentId,
                               @Extent, @Width, @Height, @Selectable, @Selected,
                               @Draggable, @Dragging, @Hidden, @Data::jsonb)
                           ON CONFLICT (id) DO UPDATE SET
                               procedure_id = EXCLUDED.procedure_id,
                               node_type = EXCLUDED.node_type,
                               position = EXCLUDED.position,
                               parent_id = EXCLUDED.parent_id,
                               extent = EXCLUDED.extent,
                               width = EXCLUDED.width,
                               height = EXCLUDED.height,
                               selectable = EXCLUDED.selectable,
                               selected = EXCLUDED.selected,
                               draggable = EXCLUDED.draggable,
                               dragging = EXCLUDED.dragging,
                               hidden = EXCLUDED.hidden,
                               data = EXCLUDED.data
                           """;

        await connection.ExecuteAsync(sql, MapNodeToParams(node));

        return node;
    }

    public async Task<bool> UpdateNodeAsync(Node node)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE nodes SET
                               procedure_id = @ProcedureId,
                               node_type = @NodeType,
                               position = @Position::jsonb,
                               parent_id = @ParentId,
                               extent = @Extent,
                               width = @Width,
                               height = @Height,
                               selectable = @Selectable,
                               selected = @Selected,
                               draggable = @Draggable,
                               dragging = @Dragging,
                               hidden = @Hidden,
                               data = @Data::jsonb
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, MapNodeToParams(node));
        return affected > 0;
    }

    public async Task<bool> UpdateMultipleNodesAsync(IReadOnlyList<Node> nodes)
    {
        if (nodes.Count == 0) return true;

        await using var connection = GetConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string sql = """
                           UPDATE nodes SET
                               procedure_id = @ProcedureId,
                               node_type = @NodeType,
                               position = @Position::jsonb,
                               parent_id = @ParentId,
                               extent = @Extent,
                               width = @Width,
                               height = @Height,
                               selectable = @Selectable,
                               selected = @Selected,
                               draggable = @Draggable,
                               dragging = @Dragging,
                               hidden = @Hidden,
                               data = @Data::jsonb
                           WHERE id = @Id
                           """;

        var totalAffected = 0;
        foreach (var node in nodes)
        {
            var affected = await connection.ExecuteAsync(sql, MapNodeToParams(node), transaction);
            totalAffected += affected;
        }

        await transaction.CommitAsync();
        return totalAffected == nodes.Count;
    }

    public async Task<bool> DeleteNodeAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM nodes WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<bool> DeleteNodesByProcedureIdAsync(Guid procedureId)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "DELETE FROM nodes WHERE procedure_id = @ProcedureId";
            var affected = await connection.ExecuteAsync(sql, new { ProcedureId = procedureId });
            return affected > 0;
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedCannotDelete("nodes", procedureId);
            return false;
        }
    }

    public async Task<List<DependencyEdge>> GetAllEdgesAsync()
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {EdgeSelectColumns} FROM dependency_edges";

            var rows = await connection.QueryAsync(sql);
            return rows.Select(MapEdgeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("dependency_edges");
            return [];
        }
    }

    public async Task<List<DependencyEdge>> GetEdgesByProcedureIdAsync(Guid procedureId)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {EdgeSelectColumns} FROM dependency_edges WHERE procedure_id = @ProcedureId";

            var rows = await connection.QueryAsync(sql, new { ProcedureId = procedureId });
            return rows.Select(MapEdgeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("dependency_edges");
            return [];
        }
    }

    public async Task<DependencyEdge?> GetEdgeByIdAsync(Guid id)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {EdgeSelectColumns} FROM dependency_edges WHERE id = @Id";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            return row is null ? null : MapEdgeFromRow(row);
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedNullResult("dependency_edge", id);
            return null;
        }
    }

    public async Task<List<DependencyEdge>> GetEdgesByIdsAsync(IReadOnlyList<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            await using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = $"SELECT {EdgeSelectColumns} FROM dependency_edges WHERE id = ANY(@Ids)";

            var rows = await connection.QueryAsync(sql, new { Ids = ids.ToArray() });
            return rows.Select(MapEdgeFromRow).ToList();
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedEmptyList("dependency_edges");
            return [];
        }
    }

    public async Task<DependencyEdge> CreateEdgeAsync(DependencyEdge edge)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO dependency_edges (id, procedure_id, source_id, target_id, source_handle, target_handle)
                           VALUES (@Id, @ProcedureId, @SourceId, @TargetId, @SourceHandle, @TargetHandle)
                           ON CONFLICT (id) DO UPDATE SET
                               procedure_id = EXCLUDED.procedure_id,
                               source_id = EXCLUDED.source_id,
                               target_id = EXCLUDED.target_id,
                               source_handle = EXCLUDED.source_handle,
                               target_handle = EXCLUDED.target_handle
                           """;

        await connection.ExecuteAsync(sql, new
        {
            edge.Id,
            edge.ProcedureId,
            edge.SourceId,
            edge.TargetId,
            edge.SourceHandle,
            edge.TargetHandle
        });

        return edge;
    }

    public async Task<bool> UpdateEdgeAsync(DependencyEdge edge)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = """
                           UPDATE dependency_edges SET
                               procedure_id = @ProcedureId,
                               source_id = @SourceId,
                               target_id = @TargetId,
                               source_handle = @SourceHandle,
                               target_handle = @TargetHandle
                           WHERE id = @Id
                           """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            edge.Id,
            edge.ProcedureId,
            edge.SourceId,
            edge.TargetId,
            edge.SourceHandle,
            edge.TargetHandle
        });

        return affected > 0;
    }

    public async Task<bool> UpdateMultipleEdgesAsync(IReadOnlyList<DependencyEdge> edges)
    {
        if (edges.Count == 0) return true;

        await using var connection = GetConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string sql = """
                           UPDATE dependency_edges SET
                               procedure_id = @ProcedureId,
                               source_id = @SourceId,
                               target_id = @TargetId,
                               source_handle = @SourceHandle,
                               target_handle = @TargetHandle
                           WHERE id = @Id
                           """;

        var totalAffected = 0;
        foreach (var entity in edges)
        {
            var affected = await connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.ProcedureId,
                entity.SourceId,
                entity.TargetId,
                entity.SourceHandle,
                entity.TargetHandle
            }, transaction);
            totalAffected += affected;
        }

        await transaction.CommitAsync();
        return totalAffected == edges.Count;
    }

    public async Task<bool> DeleteEdgeAsync(Guid id)
    {
        await using var connection = GetConnection();
        await connection.OpenAsync();

        const string sql = "DELETE FROM dependency_edges WHERE id = @Id";
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<bool> DeleteEdgesByProcedureIdAsync(Guid procedureId)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = "DELETE FROM dependency_edges WHERE procedure_id = @ProcedureId";
            var affected = await connection.ExecuteAsync(sql, new { ProcedureId = procedureId });
            return affected > 0;
        }
        catch (PostgresNotConnectedException)
        {
            Logger?.LogDisconnectedCannotDelete("dependency_edges", procedureId);
            return false;
        }
    }

    private Procedure MapProcedureFromRow(dynamic row)
    {
        var rootNodeIds = row.root_node_ids is Guid[] guids
            ? (IReadOnlyList<Guid>)guids.ToList()
            : (IReadOnlyList<Guid>)Array.Empty<Guid>();

        if (row.root_node_ids is not Guid[])
            Logger?.LogProcedureRootNodeIdsFallback((Guid)row.id);

        var variables = DeserializeFromJson<List<VariableDefinition>>(row.variables as string)
                        ?? [];

        return new Procedure
        {
            Id = row.id,
            Name = row.name,
            Description = row.description,
            CreatedAtUtc = row.created_at_utc,
            LastUpdatedAtUtc = row.last_updated_at_utc,
            RootNodeIds = rootNodeIds,
            Variables = variables,
            IsLoaded = row.is_loaded,
            LastLoadedUtc = row.last_loaded_utc
        };
    }

    private object MapNodeToParams(Node entity)
    {
        var (nodeType, data) = entity switch
        {
            TaskNode tn => (nameof(TaskNode), SerializeToJson(tn.Task)),
            SkillExecutionNode sen => (nameof(SkillExecutionNode), SerializeToJson(sen.SkillExecutionTask)),
            RouterNode rn => (nameof(RouterNode), SerializeToJson(rn.RouterTask)),
            _ => throw new ArgumentException($"Unknown Node type: {entity.GetType().Name}")
        };

        return new
        {
            entity.Id,
            entity.ProcedureId,
            NodeType = nodeType,
            Position = SerializeToJson(entity.Position),
            entity.ParentId,
            entity.Extent,
            entity.Width,
            entity.Height,
            entity.Selectable,
            entity.Selected,
            entity.Draggable,
            entity.Dragging,
            entity.Hidden,
            Data = data
        };
    }

    private Node MapNodeFromRow(dynamic row)
    {
        var position = DeserializeFromJson<NodePosition>(row.position as string)
                       ?? new NodePosition { X = 0, Y = 0 };

        string nodeType = row.node_type;
        var dataJson = row.data as string;

        Node node = nodeType switch
        {
            nameof(TaskNode) => new TaskNode
            {
                Id = row.id, ProcedureId = row.procedure_id, Position = position,
                Task = DeserializeAndWarnTaskData(dataJson, (Guid)row.id, (Guid)row.procedure_id)
            },
            nameof(SkillExecutionNode) => new SkillExecutionNode
            {
                Id = row.id, ProcedureId = row.procedure_id, Position = position,
                SkillExecutionTask = DeserializeFromJson<SkillExecutionTask>(dataJson)
                                     ?? throw new InvalidOperationException(
                                         "Failed to deserialize SkillExecutionTask data")
            },
            nameof(RouterNode) => new RouterNode
            {
                Id = row.id, ProcedureId = row.procedure_id, Position = position,
                RouterTask = DeserializeFromJson<RouterTask>(dataJson)
                             ?? throw new InvalidOperationException("Failed to deserialize RouterTask data")
            },
            _ => throw new InvalidOperationException($"Unknown node type: {nodeType}")
        };

        return node with
        {
            ParentId = row.parent_id,
            Extent = row.extent,
            Width = row.width,
            Height = row.height,
            Selectable = row.selectable,
            Selected = row.selected,
            Draggable = row.draggable,
            Dragging = row.dragging,
            Hidden = row.hidden
        };
    }

    /// <summary>
    ///     Deserializes the JSON data column for a <see cref="TaskNode" /> row and returns the result.
    ///     When the data column is null or empty and deserialization yields <see langword="null" />,
    ///     emits a <see cref="LogLevel.Warning" /> and substitutes an empty zero-duration <see cref="Task" />.
    /// </summary>
    /// <param name="dataJson">The raw JSON string from the <c>data</c> column, or <see langword="null" />.</param>
    /// <param name="nodeId">The identifier of the node being mapped, used in the warning message.</param>
    /// <param name="procedureId">The identifier of the owning procedure, used in the warning message.</param>
    /// <returns>
    ///     The deserialized <see cref="Task" />, or a zero-duration placeholder when deserialization yields
    ///     <see langword="null" />.
    /// </returns>
    private Task DeserializeAndWarnTaskData(string? dataJson, Guid nodeId, Guid procedureId)
    {
        var taskData = DeserializeFromJson<Task>(dataJson);
        if (taskData is null)
            Logger?.LogTaskNodeDataFallback(nodeId, procedureId);
        return taskData ?? new Task { Name = "", StartTime = 0, Duration = 0 };
    }

    private static DependencyEdge MapEdgeFromRow(dynamic row)
    {
        return new DependencyEdge
        {
            Id = row.id,
            ProcedureId = row.procedure_id,
            SourceId = row.source_id,
            TargetId = row.target_id,
            SourceHandle = row.source_handle,
            TargetHandle = row.target_handle
        };
    }
}