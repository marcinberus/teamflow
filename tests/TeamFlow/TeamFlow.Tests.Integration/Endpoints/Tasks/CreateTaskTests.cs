using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Tasks;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateTaskTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public CreateTaskTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenOwnerCreatesTaskForProjectMember()
    {
        var owner = await RegisterAsync();
        var assignee = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, assignee.UserId);
        UseToken(owner.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest(
                "Design API",
                "Define endpoints",
                assignee.UserId,
                DateTimeOffset.UtcNow.AddDays(1)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResult>();
        result.Should().NotBeNull();
        result!.TaskId.Should().NotBeEmpty();
        response.Headers.Location!.ToString().Should().EndWith($"/tasks/{result.TaskId}");
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenMemberOmitsAssignee()
    {
        var owner = await RegisterAsync();
        var member = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, member.UserId);
        UseToken(member.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest("Design API", string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn422_WhenTitleIsEmpty()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest(string.Empty, string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn422_WhenDueDateIsNotInFuture()
    {
        var owner = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest("Design API", string.Empty, null, DateTimeOffset.UtcNow.AddDays(-1)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn403_WhenCallerIsNotProjectMember()
    {
        var owner = await RegisterAsync();
        var caller = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(caller.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest("Design API", string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var caller = await RegisterAsync();
        UseToken(caller.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(Guid.NewGuid()),
            new CreateTaskRequest("Design API", string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn422_WhenAssigneeIsNotProjectMember()
    {
        var owner = await RegisterAsync();
        var outsider = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest("Design API", string.Empty, outsider.UserId, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(Guid.NewGuid()),
            new CreateTaskRequest("Design API", string.Empty, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterUserResult> RegisterAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"create-task-{Guid.NewGuid():N}@example.com",
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

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
