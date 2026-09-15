using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Users.Commands.RegisterUser;
using TeamFlow.Application.Users.Queries.ListUsers;

namespace TeamFlow.Tests.Integration.Endpoints.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class ListUsersTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ListUsersTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ListUsers_ShouldSearchAndPaginateTheCompleteResultSet()
    {
        var registration = await RegisterAsync("user-search-auth@example.com", "Auth", "User", "Manager");
        await RegisterAsync("alice@example.com", "Pickable", "Alpha", "Developer");
        await RegisterAsync("bob@example.com", "Pickable", "Beta", "Admin");
        await RegisterAsync("carol@example.com", "Unrelated", "Gamma", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);

        var firstResponse = await _client.GetAsync($"{Paths.Users}?search=pickable&page=1&pageSize=1");
        var secondResponse = await _client.GetAsync($"{Paths.Users}?search=pickable&page=2&pageSize=1");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<ListUsersResult>();
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<ListUsersResult>();
        firstPage.Should().NotBeNull();
        secondPage.Should().NotBeNull();
        firstPage!.TotalCount.Should().Be(2);
        secondPage!.TotalCount.Should().Be(2);
        firstPage.Items.Should().ContainSingle().Which.Email.Should().Be("alice@example.com");
        secondPage.Items.Should().ContainSingle().Which.Email.Should().Be("bob@example.com");
    }

    private async Task<RegisterUserResult> RegisterAsync(
        string email,
        string firstName,
        string lastName,
        string role)
    {
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, "P@ssw0rd!", firstName, lastName, role));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<RegisterUserResult>())!;
    }
}
