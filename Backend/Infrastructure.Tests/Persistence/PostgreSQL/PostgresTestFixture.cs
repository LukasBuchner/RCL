using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Task = System.Threading.Tasks.Task;

namespace FHOOE.Freydis.Infrastructure.Tests.Persistence.PostgreSQL;

/// <summary>
///     Shared test fixture that starts a PostgreSQL Testcontainer
///     and provides a PostgresDbContext for tests.
///     Schema migration is handled automatically by the PostgresDbContext constructor.
/// </summary>
public class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public PostgresDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var inMemorySettings = new Dictionary<string, string>
        {
            { "ConnectionStrings:PostgreSQL", _container.GetConnectionString() }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var logger = new NullLogger<PostgresDbContext>();
        Context = new PostgresDbContext(configuration, logger);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}