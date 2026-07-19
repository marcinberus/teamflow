using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Tasks.Queries.ListTasks;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Tasks;

[Collection(IntegrationTestCollection.Name)]
public sealed class ListTasksTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ListTasksTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ListTasks_ShouldReturn200_WithFilteredPaginatedResults()
    {
        var owner = await RegisterAsync();
        var assignee = await RegisterAsync();
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, owner.Token, assignee.UserId);
        UseToken(owner.Token);
        await CreateTaskAsync(projectId, "Assigned task", assignee.UserId);
        await CreateTaskAsync(projectId, "Owner task", owner.UserId);

        var response = await _client.GetAsync(
            $"{Paths.ProjectTasks(projectId)}?status=Todo&assignedUserId={assignee.UserId}&page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListTasksResult>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        var task = result.Items.Should().ContainSingle().Subject;
        task.Title.Should().Be("Assigned task");
        task.Status.Should().Be("Todo");
        task.AssignedUserId.Should().Be(assignee.UserId);
        task.AssigneeName.Should().Be("Test User");
    }

    [Fact]
    public async Task ListTasks_ShouldReturn422_WhenStatusIsInvalid()
    {
        var user = await RegisterAsync();
        UseToken(user.Token);

        var response = await _client.GetAsync(
            $"{Paths.ProjectTasks(Guid.NewGuid())}?status=Archived");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ListTasks_ShouldReturn422_WhenPageSizeExceedsMaximum()
    {
        var user = await RegisterAsync();
        UseToken(user.Token);

        var response = await _client.GetAsync(
            $"{Paths.ProjectTasks(Guid.NewGuid())}?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ListTasks_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(Paths.ProjectTasks(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterUserResult> RegisterAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"list-task-{Guid.NewGuid():N}@example.com",
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
            new CreateProjectCommand("Task list project", "Description"));

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

    private async Task CreateTaskAsync(Guid projectId, string title, Guid assignedUserId)
    {
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectTasks(projectId),
            new CreateTaskRequest(title, "Description", assignedUserId, null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
