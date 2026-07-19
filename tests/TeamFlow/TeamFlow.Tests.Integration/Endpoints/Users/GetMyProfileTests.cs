using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;
using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Tests.Integration.Endpoints.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetMyProfileTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public GetMyProfileTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password = "P@ssw0rd!")
    {
        await _client.PostAsJsonAsync(Paths.Users, new RegisterUserCommand(email, password, "Test", "User", "Developer"));
        var loginResponse = await _client.PostAsJsonAsync(Paths.Login, new LoginUserCommand(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>();
        return loginResult!.Token;
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturn200_WhenUserIsAuthenticated()
    {
        const string email = "getprofile-valid@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"{Paths.Users}/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
        profile.FirstName.Should().Be("Test");
        profile.LastName.Should().Be("User");
        profile.Role.Should().Be("Developer");
        profile.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync($"{Paths.Users}/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
