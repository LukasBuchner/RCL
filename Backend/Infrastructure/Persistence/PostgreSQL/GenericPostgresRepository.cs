using System.Text.Json;
using System.Text.Json.Serialization;
using FHOOE.Freydis.Domain;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     Abstract generic repository for PostgreSQL using Dapper.
///     Subclasses provide entity-specific SQL mapping via abstract methods.
/// </summary>
/// <typeparam name="T">The type of entity to operate on.</typeparam>
public abstract class GenericPostgresRepository<T> : IRepository<T> where T : class
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TypeHierarchyJsonConverter()
        }
    };

    protected GenericPostgresRepository(PostgresDbContext context, string tableName, ILogger? logger = null)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        TableName = tableName;
        Logger = logger;
    }

    protected PostgresDbContext Context { get; }
    protected ILogger? Logger { get; }
    protected string TableName { get; }

    public abstract Task<List<T>> GetAllAsync();
    public abstract Task<T?> GetByIdAsync(Guid id);
    public abstract Task<List<T>> GetByIdsAsync(IReadOnlyList<Guid> ids);
    public abstract Task<T> CreateAsync(T entity);
    public abstract Task<bool> UpdateAsync(T entity);

    /// <summary>
    ///     Updates multiple entities within a single database connection and transaction.
    ///     Subclasses that need entity-specific batch SQL should override this method
    ///     (see <see cref="ProcedureRepository.UpdateMultipleNodesAsync"/> for an example).
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <returns><c>true</c> if all entities were updated successfully; otherwise <c>false</c>.</returns>
    public virtual async Task<bool> UpdateMultipleAsync(IReadOnlyList<T> entities)
    {
        if (entities.Count == 0) return true;

        await using var connection = GetConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var totalAffected = 0;
            foreach (var entity in entities)
            {
                var affected = await ExecuteSingleUpdateAsync(connection, transaction, entity);
                totalAffected += affected;
            }

            await transaction.CommitAsync();
            return totalAffected == entities.Count;
        }
        catch
        {
            // Transaction is automatically rolled back on dispose if not committed
            throw;
        }
    }

    public abstract Task<bool> DeleteAsync(Guid id);

    /// <summary>
    ///     Executes a single entity update within an existing connection and transaction.
    ///     Override this method to provide entity-specific update SQL for transactional batch updates.
    ///     The default implementation falls back to <see cref="UpdateAsync"/> using a separate connection,
    ///     which does not participate in the transaction.
    /// </summary>
    /// <param name="connection">The shared database connection.</param>
    /// <param name="transaction">The active transaction.</param>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The number of rows affected (expected: 1).</returns>
    protected virtual async Task<int> ExecuteSingleUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        T entity)
    {
        // Default: delegate to the subclass's UpdateAsync.
        // Subclasses should override this for proper transactional behavior.
        var result = await UpdateAsync(entity);
        return result ? 1 : 0;
    }

    protected NpgsqlConnection GetConnection()
    {
        return Context.CreateConnection();
    }

    /// <summary>
    ///     Serializes an object to a JSON string for JSONB column storage.
    /// </summary>
    protected string SerializeToJson(object? value)
    {
        if (value is null) return "null";
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    ///     Deserializes a JSON string from a JSONB column to the specified type.
    /// </summary>
    protected TResult? DeserializeFromJson<TResult>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        return JsonSerializer.Deserialize<TResult>(json, JsonOptions);
    }
}