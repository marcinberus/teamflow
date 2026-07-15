using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class DeleteProjectTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public DeleteProjectTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn204AndRemoveProject_WhenCallerIsOwner()
    {
        var token = await RegisterAndLoginAsync("delete-project-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var projectId = await CreateProjectAsync();

        var response = await _client.DeleteAsync($"{Paths.Projects}/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"{Paths.Projects}/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn204_WhenCallerIsAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("delete-project-admin-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var adminToken = await RegisterAndLoginAsync("delete-project-admin@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.DeleteAsync($"{Paths.Projects}/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn403AndKeepProject_WhenCallerIsNotOwnerOrAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("delete-project-forbidden-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var otherToken = await RegisterAndLoginAsync("delete-project-forbidden-other@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.DeleteAsync($"{Paths.Projects}/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var getResponse = await _client.GetAsync($"{Paths.Projects}/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var token = await RegisterAndLoginAsync("delete-project-missing@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"{Paths.Projects}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn422_WhenProjectIdIsEmpty()
    {
        var token = await RegisterAndLoginAsync("delete-project-invalid@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"{Paths.Projects}/{Guid.Empty}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync($"{Paths.Projects}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync(
            Paths.Projects,
            new CreateProjectCommand("Apollo", "Landing mission"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateProjectResult>();
        return result!.ProjectId;
    }

    private async Task<string> RegisterAndLoginAsync(
        string email,
        string role,
        string password = "P@ssw0rd!")
    {
        var registerResponse = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, password, "Test", "User", role));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync(Paths.Login, new LoginUserCommand(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>();
        return result!.Token;
    }
}
