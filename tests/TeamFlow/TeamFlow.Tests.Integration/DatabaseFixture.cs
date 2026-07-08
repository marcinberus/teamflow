using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

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
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
