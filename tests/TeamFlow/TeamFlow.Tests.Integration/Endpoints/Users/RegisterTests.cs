using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Users;

public sealed class RegisterTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public RegisterTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturn201_WhenInputIsValid()
    {
        var command = new RegisterUserCommand(
            "alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<RegisterUserResult>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_ShouldReturn422_WhenEmailIsInvalid()
    {
        var command = new RegisterUserCommand(
            "not-an-email", "P@ssw0rd!", "Alice", "Smith", "Developer");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_ShouldReturn422_WhenPasswordIsTooWeak()
    {
        var command = new RegisterUserCommand(
            "bob@example.com", "weakpass", "Bob", "Jones", "Developer");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_ShouldReturn422_WhenPasswordHasNoSpecialChar()
    {
        var command = new RegisterUserCommand(
            "bob@example.com", "Password1", "Bob", "Jones", "Developer");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_ShouldReturn422_WhenRoleIsInvalid()
    {
        var command = new RegisterUserCommand(
            "carol@example.com", "P@ssw0rd!", "Carol", "White", "SuperAdmin");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_ShouldReturn409_WhenEmailAlreadyExists()
    {
        var command = new RegisterUserCommand(
            "duplicate@example.com", "P@ssw0rd!", "Dave", "Brown", "Manager");

        await _client.PostAsJsonAsync("/api/v1/users", command);
        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturn422_WhenFirstNameIsEmpty()
    {
        var command = new RegisterUserCommand(
            "eve@example.com", "P@ssw0rd!", "", "Adams", "Developer");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_ShouldReturnLocationHeader_WhenCreated()
    {
        var command = new RegisterUserCommand(
            "frank@example.com", "P@ssw0rd!", "Frank", "Lee", "Admin");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}
