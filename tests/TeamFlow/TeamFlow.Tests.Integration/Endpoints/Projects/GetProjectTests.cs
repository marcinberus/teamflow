using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

[Collection(IntegrationTestCollection.Name)]
public sealed class GetProjectTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public GetProjectTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetProject_ShouldReturn200_WhenProjectExists()
    {
        var token = await RegisterAndLoginAsync("get-project-valid@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            Paths.Projects,
            new CreateProjectCommand("Apollo", "Landing mission"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProjectResult>();

        var response = await _client.GetAsync($"{Paths.Projects}/{createResult!.ProjectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProjectDetailsDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createResult.ProjectId);
        result.Name.Should().Be("Apollo");
        result.Description.Should().Be("Landing mission");
        result.Status.Should().Be("Active");
        result.OwnerName.Should().Be("Test User");
        result.CreatedAt.Should().NotBe(default);
        result.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var token = await RegisterAndLoginAsync("get-project-missing@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"{Paths.Projects}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProject_ShouldReturn404_WhenProjectIdIsNotAGuid()
    {
        var response = await _client.GetAsync($"{Paths.Projects}/123");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password = "P@ssw0rd!")
    {
        var registerResponse = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, password, "Test", "User", "Developer"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync(Paths.Login, new LoginUserCommand(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>();
        return result!.Token;
    }
}
