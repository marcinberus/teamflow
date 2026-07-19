namespace TeamFlow.Tests.Integration;

public static class IntegrationTestCollection
{
    public const string Name = "Integration";
}

[CollectionDefinition(IntegrationTestCollection.Name, DisableParallelization = true)]
public sealed class IntegrationTestCollectionDefinition
    : ICollectionFixture<IntegrationTestFixture>
{
}
