using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Commands.UpdateProject;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class UpdateProjectTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public UpdateProjectTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn204AndPersistChanges_WhenCallerIsOwner()
    {
        var token = await RegisterAndLoginAsync("update-project-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var projectId = await CreateProjectAsync();

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{projectId}",
            new UpdateProjectRequest("Apollo v2", "Updated mission"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"{Paths.Projects}/{projectId}");
        var project = await getResponse.Content.ReadFromJsonAsync<ProjectDetailsDto>();
        project.Should().NotBeNull();
        project!.Name.Should().Be("Apollo v2");
        project.Description.Should().Be("Updated mission");
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn204_WhenCallerIsAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("update-project-admin-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var adminToken = await RegisterAndLoginAsync("update-project-admin@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{projectId}",
            new UpdateProjectRequest("Admin update", "Updated by admin"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn403_WhenCallerIsNotOwnerOrAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("update-project-forbidden-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var otherToken = await RegisterAndLoginAsync("update-project-forbidden-other@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{projectId}",
            new UpdateProjectRequest("Unauthorized update", "Should not persist"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var token = await RegisterAndLoginAsync("update-project-missing@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{Guid.NewGuid()}",
            new UpdateProjectRequest("Apollo v2", "Updated mission"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn422_WhenNameIsEmpty()
    {
        var token = await RegisterAndLoginAsync("update-project-invalid@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var projectId = await CreateProjectAsync();

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{projectId}",
            new UpdateProjectRequest(string.Empty, "Updated mission"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync(
            $"{Paths.Projects}/{Guid.NewGuid()}",
            new UpdateProjectRequest("Apollo v2", "Updated mission"));

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
