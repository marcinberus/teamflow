using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<TeamFlowDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new TeamFlowDbContext(options);
        await db.Database.MigrateAsync();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
    }

    public async Task ResetAsync()
    {
        ArgumentNullException.ThrowIfNull(_respawner);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
