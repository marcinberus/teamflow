using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;
using TeamFlow.Application.Users.Commands.UpdateProfile;
using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Tests.Integration.Endpoints.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateProfileTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public UpdateProfileTests(IntegrationTestFixture fixture) : base(fixture)
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
    public async Task UpdateProfile_ShouldReturn204_WhenInputIsValid()
    {
        const string email = "updateprofile-valid@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var command = new UpdateProfileCommand("Alicia", "Jones");
        var response = await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateProfileInDatabase_WhenInputIsValid()
    {
        const string email = "updateprofile-update@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var command = new UpdateProfileCommand("Alicia", "Jones");
        await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        var getResponse = await _client.GetAsync($"{Paths.Users}/me");
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.FirstName.Should().Be("Alicia");
        profile.LastName.Should().Be("Jones");
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn422_WhenFirstNameIsEmpty()
    {
        const string email = "updateprofile-empty-first@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var command = new UpdateProfileCommand("", "Jones");
        var response = await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn422_WhenLastNameIsEmpty()
    {
        const string email = "updateprofile-empty-last@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var command = new UpdateProfileCommand("Alicia", "");
        var response = await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn401_WhenNotAuthenticated()
    {
        var command = new UpdateProfileCommand("Alicia", "Jones");
        var response = await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn422_WhenFirstNameExceedsMaxLength()
    {
        const string email = "updateprofile-toolong@example.com";
        var token = await RegisterAndLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var command = new UpdateProfileCommand(new string('A', 101), "Jones");
        var response = await _client.PutAsJsonAsync($"{Paths.Users}/me", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
