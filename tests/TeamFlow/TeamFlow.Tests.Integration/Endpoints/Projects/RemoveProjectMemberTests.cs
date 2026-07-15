using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Queries.ListProjectMembers;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class RemoveProjectMemberTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public RemoveProjectMemberTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn204AndRemoveMember_WhenCallerIsOwner()
    {
        var owner = await RegisterAsync("Developer");
        var member = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, member.UserId, owner.Token);

        UseToken(owner.Token);
        var response = await _client.DeleteAsync(Paths.ProjectMember(projectId, member.UserId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var listResponse = await _client.GetAsync(Paths.ProjectMembers(projectId));
        var list = await listResponse.Content.ReadFromJsonAsync<ListProjectMembersResult>();
        list!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn204_WhenCallerIsManager()
    {
        var owner = await RegisterAsync("Developer");
        var manager = await RegisterAsync("Manager");
        var member = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, member.UserId, owner.Token);

        UseToken(manager.Token);
        var response = await _client.DeleteAsync(Paths.ProjectMember(projectId, member.UserId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn403_WhenCallerCannotManageMembers()
    {
        var owner = await RegisterAsync("Developer");
        var caller = await RegisterAsync("Developer");
        var member = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        await AssignMemberAsync(projectId, member.UserId, owner.Token);

        UseToken(caller.Token);
        var response = await _client.DeleteAsync(Paths.ProjectMember(projectId, member.UserId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn409_WhenTargetUserIsProjectOwner()
    {
        var owner = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(owner.Token);
        var response = await _client.DeleteAsync(Paths.ProjectMember(projectId, owner.UserId));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var manager = await RegisterAsync("Manager");
        UseToken(manager.Token);

        var response = await _client.DeleteAsync(
            Paths.ProjectMember(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn404_WhenMemberDoesNotExist()
    {
        var owner = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.DeleteAsync(
            Paths.ProjectMember(projectId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn422_WhenUserIdIsEmpty()
    {
        var owner = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.DeleteAsync(Paths.ProjectMember(projectId, Guid.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync(
            Paths.ProjectMember(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisteredUser> RegisterAsync(string role)
    {
        var email = $"remove-member-{Guid.NewGuid():N}@example.com";
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, "P@ssw0rd!", "Test", "User", role));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<RegisterUserResult>())!;

        return new RegisteredUser(result.UserId, result.Token);
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

    private async Task AssignMemberAsync(Guid projectId, Guid userId, string token)
    {
        UseToken(token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(userId, "Developer"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record RegisteredUser(Guid UserId, string Token);
}
