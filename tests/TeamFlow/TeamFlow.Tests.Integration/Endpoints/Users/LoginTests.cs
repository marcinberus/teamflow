using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Users;

[Collection(IntegrationTestCollection.Name)]
public sealed class LoginTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public LoginTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    private async Task RegisterUserAsync(string email, string password = "P@ssw0rd!")
    {
        var register = new RegisterUserCommand(email, password, "Test", "User", "Developer");
        await _client.PostAsJsonAsync(Paths.Users, register);
    }

    [Fact]
    public async Task Login_ShouldReturn200_WhenCredentialsAreValid()
    {
        const string email = "login-valid@example.com";
        const string password = "P@ssw0rd!";
        await RegisterUserAsync(email, password);

        var command = new LoginUserCommand(email, password);
        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginUserResult>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.UserId.Should().NotBeEmpty();
        result.Role.Should().Be("Developer");
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenEmailNotFound()
    {
        var command = new LoginUserCommand("nonexistent@example.com", "P@ssw0rd!");

        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn401_WhenPasswordIsWrong()
    {
        const string email = "login-wrong-pass@example.com";
        await RegisterUserAsync(email);

        var command = new LoginUserCommand(email, "WrongPass999!");
        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturn422_WhenEmailIsEmpty()
    {
        var command = new LoginUserCommand("", "P@ssw0rd!");

        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_ShouldReturn422_WhenPasswordIsEmpty()
    {
        var command = new LoginUserCommand("alice@example.com", "");

        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_ShouldReturn422_WhenEmailIsInvalidFormat()
    {
        var command = new LoginUserCommand("not-an-email", "P@ssw0rd!");

        var response = await _client.PostAsJsonAsync(Paths.Login, command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
