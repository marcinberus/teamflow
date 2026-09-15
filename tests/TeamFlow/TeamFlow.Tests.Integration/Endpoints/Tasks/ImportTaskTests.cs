using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Integration.Endpoints.Tasks;

[Collection(IntegrationTestCollection.Name)]
public sealed class ImportTaskTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ImportTaskTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ImportTask_ShouldMapFailedResult()
    {
        await AuthenticateAsync("import-task-not-found@example.com");
        using var content = CreateImportContent(
            "tasks.csv",
            "\"Task\",\"Description\",\"\",\"\",\"Todo\"");

        var response = await _client.PostAsync(Paths.ProjectTaskImport(Guid.NewGuid()), content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ImportTask_ShouldReturn422ProblemDetails_WhenCsvRowIsInvalid()
    {
        await AuthenticateAsync("import-task-invalid-csv@example.com");
        var createResponse = await _client.PostAsJsonAsync(
            Paths.Projects,
            new CreateProjectCommand("Import project", "Description"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = await createResponse.Content.ReadFromJsonAsync<CreateProjectResult>();
        using var content = CreateImportContent("tasks.csv", "Invalid");

        var response = await _client.PostAsync(Paths.ProjectTaskImport(project!.ProjectId), content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString().Should().Be("Import validation failed");
        problem.RootElement.GetProperty("detail").GetString().Should().Be("Invalid Csv row at line 1.");
        problem.RootElement.TryGetProperty("errors", out _).Should().BeFalse();
    }

    private async Task AuthenticateAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, "P@ssw0rd!", "Test", "User", "Developer"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var registration = await response.Content.ReadFromJsonAsync<RegisterUserResult>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registration!.Token);
    }

    private static MultipartFormDataContent CreateImportContent(string fileName, string contents)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(contents)), "file", fileName);
        return content;
    }
}
