using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.ChangeProjectStatus;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class ChangeProjectStatusTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public ChangeProjectStatusTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn204AndPersistChanges_WhenCallerIsOwner()
    {
        var token = await RegisterAndLoginAsync("change-status-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var projectId = await CreateProjectAsync();

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{projectId}/status",
            new ChangeProjectStatusRequest("OnHold"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"{Paths.Projects}/{projectId}");
        var project = await getResponse.Content.ReadFromJsonAsync<ProjectDetailsDto>();
        project.Should().NotBeNull();
        project!.Status.Should().Be("OnHold");
        project.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn204_WhenCallerIsAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("change-status-admin-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var adminToken = await RegisterAndLoginAsync("change-status-admin@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{projectId}/status",
            new ChangeProjectStatusRequest("Completed"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn403_WhenCallerIsNotOwnerOrAdmin()
    {
        var ownerToken = await RegisterAndLoginAsync("change-status-forbidden-owner@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var projectId = await CreateProjectAsync();
        var otherToken = await RegisterAndLoginAsync("change-status-forbidden-other@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{projectId}/status",
            new ChangeProjectStatusRequest("OnHold"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var token = await RegisterAndLoginAsync("change-status-missing@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{Guid.NewGuid()}/status",
            new ChangeProjectStatusRequest("OnHold"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn422_WhenStatusIsInvalid()
    {
        var token = await RegisterAndLoginAsync("change-status-invalid@example.com", "Developer");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var projectId = await CreateProjectAsync();

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{projectId}/status",
            new ChangeProjectStatusRequest("Unknown"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeProjectStatus_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync(
            $"{Paths.Projects}/{Guid.NewGuid()}/status",
            new ChangeProjectStatusRequest("OnHold"));

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
