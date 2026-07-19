using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Queries.ListProjectMembers;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

[Collection(IntegrationTestCollection.Name)]
public sealed class ListProjectMembersTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ListProjectMembersTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ListMembers_ShouldReturn200_WithAssignedMembers()
    {
        var owner = await RegisterAsync("Developer");
        var target = await RegisterAsync("Manager");
        var projectId = await CreateProjectAsync(owner.Token);

        UseToken(owner.Token);
        var assignResponse = await _client.PostAsJsonAsync(
            Paths.ProjectMembers(projectId),
            new AssignMemberRequest(target.UserId, "Developer"));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync(Paths.ProjectMembers(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListProjectMembersResult>();
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        var member = result.Items.Single();
        member.MemberId.Should().NotBeEmpty();
        member.UserId.Should().Be(target.UserId);
        member.Email.Should().Be(target.Email);
        member.FirstName.Should().Be("Test");
        member.LastName.Should().Be("User");
        member.Role.Should().Be("Manager");
        member.ProjectRole.Should().Be("Developer");
        member.JoinedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task ListMembers_ShouldReturn200_WithEmptyItems_WhenNoMembersAreAssigned()
    {
        var owner = await RegisterAsync("Developer");
        var projectId = await CreateProjectAsync(owner.Token);
        UseToken(owner.Token);

        var response = await _client.GetAsync(Paths.ProjectMembers(projectId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListProjectMembersResult>();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListMembers_ShouldReturn401_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(Paths.ProjectMembers(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListMembers_ShouldReturn422_WhenProjectIdIsEmpty()
    {
        var token = (await RegisterAsync("Developer")).Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(Paths.ProjectMembers(Guid.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<RegisteredUser> RegisterAsync(string role)
    {
        var email = $"list-members-{Guid.NewGuid():N}@example.com";
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(
                email,
                "P@ssw0rd!",
                "Test",
                "User",
                role));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var registration = (await response.Content.ReadFromJsonAsync<RegisterUserResult>())!;

        return new RegisteredUser(registration.UserId, email, registration.Token);
    }

    private async Task<Guid> CreateProjectAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

    private sealed record RegisteredUser(Guid UserId, string Email, string Token);
}
