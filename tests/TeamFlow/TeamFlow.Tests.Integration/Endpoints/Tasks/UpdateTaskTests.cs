using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Tasks.Commands.UpdateTask;
using TeamFlow.Application.Tasks.Queries.ListTasks;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Tasks;

[Collection(IntegrationTestCollection.Name)]
public sealed class UpdateTaskTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public UpdateTaskTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn204AndPersistChanges_WhenCallerAndAssigneeAreMembers()
    {
        var owner = await RegisterAsync();
        var assignee = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, assignee.UserId);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        var dueDate = DateTimeOffset.UtcNow.AddDays(2);
        UseToken(owner.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            new UpdateTaskRequest(
                "Design REST API",
                "Define REST endpoints",
                assignee.UserId,
                dueDate));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(Paths.ProjectTasks(projectId));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await getResponse.Content.ReadFromJsonAsync<ListTasksResult>();
        var updatedTask = tasks!.Items.Should().ContainSingle(item => item.Id == taskId).Subject;
        updatedTask.Title.Should().Be("Design REST API");
        updatedTask.Description.Should().Be("Define REST endpoints");
        updatedTask.AssignedUserId.Should().Be(assignee.UserId);
        updatedTask.DueDate.Should().BeCloseTo(dueDate, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn204_WhenCallerIsAssignedProjectMember()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, member.UserId);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(member.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn403_WhenCallerIsNotProjectMember()
    {
        var owner = await RegisterAsync();
        var caller = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(caller.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn404_WhenTaskDoesNotExist()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, Guid.NewGuid()),
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn404_WhenTaskBelongsToAnotherProject()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var otherProjectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(otherProjectId, owner.Token);
        UseToken(owner.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn422_WhenAssigneeIsNotProjectMember()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(owner.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            new UpdateTaskRequest("Design REST API", string.Empty, outsider.UserId, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn422_WhenTitleIsEmpty()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        var taskId = await CreateTaskAsync(projectId, owner.Token);
        UseToken(owner.Token);

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(projectId, taskId),
            new UpdateTaskRequest(string.Empty, string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsJsonAsync(
            Paths.ProjectTask(Guid.NewGuid(), Guid.NewGuid()),
            ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static UpdateTaskRequest ValidRequest() =>
        new("Design REST API", "Define REST endpoints", null, null);

    private async Task<RegisterUserResult> RegisterAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"update-task-{Guid.NewGuid():N}@example.com",
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
            new CreateProjectCommand("Apollo", "Landing mission"));

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
