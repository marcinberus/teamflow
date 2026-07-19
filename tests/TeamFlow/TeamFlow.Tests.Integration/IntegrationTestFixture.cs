namespace TeamFlow.Tests.Integration;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; } = new();
    public TeamFlowWebAppFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Database.InitializeAsync();
        Factory = new TeamFlowWebAppFactory(Database);
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await Database.DisposeAsync();
    }
}
