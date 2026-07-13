using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Queries.ListProjects;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

public sealed class ListProjectsTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly HttpClient _client;

    public ListProjectsTests(TeamFlowWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListProjects_ShouldReturn200_WithPaginatedResults()
    {
        var token = await RegisterAndLoginAsync("list-projects-valid@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateProjectAsync("List project one");
        await CreateProjectAsync("List project two");

        var response = await _client.GetAsync($"{Paths.Projects}?page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListProjectsResult>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(project => project.Name == "List project one");
        result.Items.Should().Contain(project => project.Name == "List project two");
    }

    [Fact]
    public async Task ListProjects_ShouldReturn422_WhenPageSizeExceedsMaximum()
    {
        var token = await RegisterAndLoginAsync("list-projects-large-page@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"{Paths.Projects}?page=1&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ListProjects_ShouldReturn422_WhenStatusIsInvalid()
    {
        var token = await RegisterAndLoginAsync("list-projects-invalid-status@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"{Paths.Projects}?status=Archived");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task CreateProjectAsync(string name)
    {
        var response = await _client.PostAsJsonAsync(Paths.Projects, new CreateProjectCommand(name, "Description"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<string> RegisterAndLoginAsync(string email, string password = "P@ssw0rd!")
    {
        var registerResponse = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, password, "Test", "User", "Developer"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _client.PostAsJsonAsync(Paths.Login, new LoginUserCommand(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>();
        return result!.Token;
    }
}
