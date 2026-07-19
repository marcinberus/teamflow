using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

[Collection(IntegrationTestCollection.Name)]
public sealed class AssignProjectMemberTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public AssignProjectMemberTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task AssignMember_ShouldReturn201_WhenCurrentUserIsProjectOwner()
    {
        var owner = await RegisterAsync("Developer");
        var target = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(owner.Token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(target.UserId, "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<AssignMemberResult>();
        result.Should().NotBeNull();
        result!.MemberId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssignMember_ShouldReturn422_WhenRoleIsInvalid()
    {
        var owner = await RegisterAsync("Developer");
        var target = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(owner.Token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(target.UserId, "Contributor"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AssignMember_ShouldReturn403_WhenCurrentUserIsNotOwnerOrManager()
    {
        var owner = await RegisterAsync("Developer");
        var caller = await RegisterAsync("Developer");
        var target = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(caller.Token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(target.UserId, "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignMember_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var caller = await RegisterAsync("Manager");
        var target = await RegisterAsync("Developer");

        UseToken(caller.Token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(Guid.NewGuid()),
            new AssignMemberRequest(target.UserId, "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignMember_ShouldReturn404_WhenTargetUserDoesNotExist()
    {
        var owner = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(owner.Token);
        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(Guid.NewGuid(), "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignMember_ShouldReturn409_WhenUserIsAlreadyMember()
    {
        var owner = await RegisterAsync("Developer");
        var target = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        var request = new AssignMemberRequest(target.UserId, "Developer");

        UseToken(owner.Token);
        var firstResponse = await _client.PostAsJsonAsync(Paths.ProjectMembers(projectId), request);
        var secondResponse = await _client.PostAsJsonAsync(Paths.ProjectMembers(projectId), request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AssignMember_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(Guid.NewGuid()),
            new AssignMemberRequest(Guid.NewGuid(), "Developer"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<RegisterUserResult> RegisterAsync(string role)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                $"assign-member-{Guid.NewGuid():N}@example.com",
                "P@ssw0rd!",
                "Test",
                "User",
                role));

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

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
