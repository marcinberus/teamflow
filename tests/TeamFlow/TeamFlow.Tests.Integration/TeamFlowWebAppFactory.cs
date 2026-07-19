using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration;

public sealed class TeamFlowWebAppFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = database.ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TeamFlowDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<TeamFlowDbContext>(options =>
                options.UseSqlServer(database.ConnectionString));
        });
    }
}
