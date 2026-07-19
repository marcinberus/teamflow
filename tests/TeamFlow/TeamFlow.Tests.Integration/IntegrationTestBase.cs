namespace TeamFlow.Tests.Integration;

public abstract class IntegrationTestBase(IntegrationTestFixture fixture) : IAsyncLifetime
{
    protected DatabaseFixture Database => fixture.Database;

    public virtual Task InitializeAsync()
    {
        return Database.ResetAsync();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
