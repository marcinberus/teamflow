using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Tasks.Queries.ListTasks;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Tasks;

[Collection(IntegrationTestCollection.Name)]
public sealed class ChangeTaskStatusTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ChangeTaskStatusTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn204AndPersistChanges_WhenCallerIsProjectMember()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, member.UserId);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(member.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(projectId, taskId),
            new ChangeTaskStatusRequest("Done"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"{Paths.ProjectTasks(projectId)}?status=Done");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await getResponse.Content.ReadFromJsonAsync<ListTasksResult>();
        var changedTask = tasks!.Items.Should().ContainSingle(item => item.Id == taskId).Subject;
        changedTask.Status.Should().Be("Done");
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn403_WhenCallerIsNotProjectMember()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(outsider.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(projectId, taskId),
            new ChangeTaskStatusRequest("InProgress"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var caller = await RegisterAsync();
        UseToken(caller.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(Guid.NewGuid(), Guid.NewGuid()),
            new ChangeTaskStatusRequest("InProgress"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn404_WhenTaskDoesNotExist()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(projectId, Guid.NewGuid()),
            new ChangeTaskStatusRequest("InProgress"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn404_WhenTaskBelongsToAnotherProject()
    {
        var owner = await RegisterAsync();
        var firstProjectId = await CreateProjectAsync(owner.Token);
        var secondProjectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(firstProjectId, owner.Token);
        UseToken(owner.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(secondProjectId, taskId),
            new ChangeTaskStatusRequest("InProgress"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn422_WhenStatusIsInvalid()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(owner.Token);

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(projectId, taskId),
            new ChangeTaskStatusRequest("Archived"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeTaskStatus_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync(
            Paths.ProjectTaskStatus(Guid.NewGuid(), Guid.NewGuid()),
            new ChangeTaskStatusRequest("Done"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterUserResult> RegisterAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"change-task-status-{Guid.NewGuid():N}@example.com",
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
            new CreateProjectCommand("Task status project", "Description"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateProjectResult>();
        return result!.ProjectId;
    }

    private async Task AssignMemberAsync(Guid projectId, string ownerToken, Guid userId)
    {
        UseToken(ownerToken);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(userId, "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> CreateTaskAsync(Guid projectId, string token)
    {
        UseToken(token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest("Design API", "Define endpoints", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResult>();
        return result!.TaskId;
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
