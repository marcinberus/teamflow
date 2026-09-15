using FluentAssertions;

namespace TeamFlow.Tests.Integration.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public sealed class CorsTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public CorsTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task DevelopmentCors_ShouldAllowAngularDevOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Paths.Projects);
        request.Headers.Add("Origin", "http://localhost:4200");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().Equal("http://localhost:4200");
    }
}
