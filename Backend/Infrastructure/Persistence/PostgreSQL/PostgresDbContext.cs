using System.Reflection;
using FHOOE.Freydis.Infrastructure.Support.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FHOOE.Freydis.Infrastructure.Persistence.PostgreSQL;

/// <summary>
///     Thrown when a repository operation is attempted while PostgreSQL is not connected.
/// </summary>
public sealed class PostgresNotConnectedException()
    : InvalidOperationException("PostgreSQL is not connected.");

/// <summary>
///     PostgreSQL database context providing connection management and automatic schema migration using Npgsql.
/// </summary>
public class PostgresDbContext
{
    private const string MigrationsSubDirectory = "Persistence/PostgreSQL/Migrations";
    private const string InitialSchemaMigration = "001_initial_schema.sql";

    private readonly string _connectionString;
    private readonly ILogger<PostgresDbContext> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgresDbContext"/> class,
    ///     tests connectivity, and runs database schema migrations.
    /// </summary>
    /// <param name="configuration">Application configuration containing the PostgreSQL connection string.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public PostgresDbContext(IConfiguration configuration, ILogger<PostgresDbContext> logger)
    {
        _logger = logger;
        IsConnected = false;

        try
        {
            logger.LogConnecting();

            _connectionString = configuration.GetConnectionString("PostgreSQL")
                                ?? throw new InvalidOperationException(
                                    "PostgreSQL connection string is not configured");

            // Test connection
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            IsConnected = true;
            logger.LogConnected();

            // Run migrations after successful connection
            RunMigrations(conn);
        }
        catch (Exception ex) when (ex is not InvalidOperationException ||
                                   ex.Message != "PostgreSQL connection string is not configured")
        {
            logger.LogConnectionFailed(ex);
            IsConnected = false;
            _connectionString = string.Empty;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the context has an active connection to PostgreSQL.
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    ///     Creates a new <see cref="NpgsqlConnection"/> using the configured connection string.
    /// </summary>
    /// <returns>A new, unopened database connection.</returns>
    /// <exception cref="PostgresNotConnectedException">Thrown when PostgreSQL is not connected.</exception>
    public NpgsqlConnection CreateConnection()
    {
        if (!IsConnected)
            throw new PostgresNotConnectedException();
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    ///     Runs all SQL migration files from the Migrations directory in alphabetical order.
    ///     The initial schema migration (001) is wrapped with IF NOT EXISTS guards for idempotency.
    ///     Subsequent migrations are expected to be idempotent on their own (e.g. using
    ///     <c>DROP CONSTRAINT IF EXISTS</c>).
    /// </summary>
    /// <param name="connection">An open PostgreSQL connection.</param>
    private void RunMigrations(NpgsqlConnection connection)
    {
        var migrationFiles = LoadMigrationFiles();
        if (migrationFiles.Count == 0)
        {
            _logger.LogMigrationFileNotFound();
            return;
        }

        foreach (var (fileName, sql) in migrationFiles)
            try
            {
                // The initial schema uses CREATE TABLE which needs IF NOT EXISTS wrapping
                var effectiveSql = fileName == InitialSchemaMigration
                    ? MakeIdempotent(sql)
                    : sql;

                using var cmd = new NpgsqlCommand(effectiveSql, connection);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogMigrationFailed(ex);
                return;
            }

        _logger.LogMigrationCompleted();
    }

    /// <summary>
    ///     Transforms migration SQL to be idempotent by adding IF NOT EXISTS guards
    ///     to CREATE TABLE and CREATE INDEX statements.
    /// </summary>
    /// <param name="sql">The original migration SQL.</param>
    /// <returns>The idempotent migration SQL.</returns>
    private static string MakeIdempotent(string sql)
    {
        return sql
            .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase)
            .Replace("CREATE INDEX ", "CREATE INDEX IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Discovers all SQL migration files from the Migrations directory on disk,
    ///     sorted alphabetically by file name. Searches relative to the assembly location
    ///     (deployed layout) and walks up directories (development layout).
    /// </summary>
    /// <returns>
    ///     An ordered list of <c>(fileName, sql)</c> tuples, or an empty list if the
    ///     migrations directory was not found.
    /// </returns>
    private List<(string FileName, string Sql)> LoadMigrationFiles()
    {
        var migrationsDir = FindMigrationsDirectory();
        if (migrationsDir is null)
            return [];

        var files = Directory.GetFiles(migrationsDir, "*.sql");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        var result = new List<(string, string)>(files.Length);
        foreach (var filePath in files)
        {
            _logger.LogMigrationFileFound(filePath);
            result.Add((Path.GetFileName(filePath), File.ReadAllText(filePath)));
        }

        return result;
    }

    /// <summary>
    ///     Locates the Migrations directory by checking the deployed layout (next to the assembly)
    ///     and walking up parent directories (development layout).
    /// </summary>
    /// <returns>The full path to the Migrations directory, or <c>null</c> if not found.</returns>
    private static string? FindMigrationsDirectory()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyDir is null) return null;

        // Deployed layout: next to the assembly
        var candidate = Path.Combine(assemblyDir, MigrationsSubDirectory);
        if (Directory.Exists(candidate))
            return candidate;

        // Development layout: walk up directories
        var dir = assemblyDir;
        while (dir is not null)
        {
            candidate = Path.Combine(dir, "Infrastructure", "Persistence", "PostgreSQL", "Migrations");
            if (Directory.Exists(candidate))
                return candidate;

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}