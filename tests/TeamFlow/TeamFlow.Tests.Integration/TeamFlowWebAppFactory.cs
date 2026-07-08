using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration;

public sealed class TeamFlowWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly DatabaseFixture _database = new();

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TeamFlowDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<TeamFlowDbContext>(options =>
                options.UseSqlServer(_database.ConnectionString));
        });
    }
}
