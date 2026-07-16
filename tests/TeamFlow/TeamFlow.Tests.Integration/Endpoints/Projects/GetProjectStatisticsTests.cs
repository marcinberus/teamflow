using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class GetProjectStatisticsTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public GetProjectStatisticsTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturn200_WithAggregatedMetrics()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, member.UserId);
        var doneTaskId = await CreateTaskAsync(projectId, owner.Token, "Done task");
        var inProgressTaskId = await CreateTaskAsync(projectId, owner.Token, "In progress task");
        await CreateTaskAsync(projectId, owner.Token, "Todo task");
        await ChangeTaskStatusAsync(projectId, doneTaskId, owner.Token, "Done");
        await ChangeTaskStatusAsync(projectId, inProgressTaskId, owner.Token, "InProgress");
        UseToken(owner.Token);

        var response = await _client.GetAsync(Paths.ProjectStatistics(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProjectStatisticsDto>();
        result.Should().NotBeNull();
        result!.ProjectId.Should().Be(projectId);
        result.TotalTasks.Should().Be(3);
        result.TasksByStatus.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Todo"] = 1,
            ["InProgress"] = 1,
            ["Verification"] = 0,
            ["Done"] = 1,
            ["Cancelled"] = 0
        });
        result.TotalMembers.Should().Be(1);
        result.CompletionPercentage.Should().Be("33.33");
    }

    [Fact]
    public async Task GetStatistics_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var owner = await RegisterAsync();
        UseToken(owner.Token);

        var response = await _client.GetAsync(Paths.ProjectStatistics(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(Paths.ProjectStatistics(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterUserResult> RegisterAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"project-statistics-{Guid.NewGuid():N}@example.com",
                "P@ssw0rd!",
                "Test",
                "User",
                "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RegisterUserResult>())!;
    }

    private async Task<Guid> CreateProjectAsync(string token)
    {
        UseToken(token);
        var response = await _client.PostAsJsonAsync(
            Paths.Projects,
            new CreateProjectCommand("Statistics project", "Description"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateProjectResult>();
        return result!.ProjectId;
    }

    private async Task AssignMemberAsync(Guid projectId, string token, Guid userId)
    {
        UseToken(token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(userId, "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> CreateTaskAsync(Guid projectId, string token, string title)
    {
        UseToken(token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest(title, "Description", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResult>();
        return result!.TaskId;
    }

    private async Task ChangeTaskStatusAsync(
        Guid projectId,
        Guid taskId,
        string token,
        string status)
    {
        UseToken(token);
        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(projectId, taskId),
            new ChangeTaskStatusRequest(status));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
