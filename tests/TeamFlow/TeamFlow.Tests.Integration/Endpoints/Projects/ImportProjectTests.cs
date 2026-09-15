using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Projects.Commands.ImportProject;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Commands.RegisterUser;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration.Endpoints.Projects;

[Collection(IntegrationTestCollection.Name)]
public sealed class ImportProjectTests : IntegrationTestBase
{
    private readonly HttpClient _client;

    public ImportProjectTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ImportProject_ShouldReturn201AndPersistProjects_WhenCsvFileIsValid()
    {
        var registration = await RegisterAndLoginAsync("import-project-valid@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);

        using var content = CreateImportContent(
            "projects.csv",
            "\"Apollo\",\"Landing mission\"\r\n\"Orion\",\"Deep space exploration\"");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var result = await response.Content.ReadFromJsonAsync<ImportProjectResult>();
        result.Should().NotBeNull();
        var importedProjectIds = result!.ProjectIds.ToArray();
        importedProjectIds.Should().HaveCount(2);

        var options = new DbContextOptionsBuilder<TeamFlowDbContext>()
            .UseSqlServer(Database.ConnectionString)
            .Options;
        await using var db = new TeamFlowDbContext(options);
        var projects = await db.Projects
            .AsNoTracking()
            .Where(project => importedProjectIds.Contains(project.Id))
            .OrderBy(project => project.Name)
            .ToListAsync();

        projects.Should().HaveCount(2);
        projects.Should().AllSatisfy(project => project.OwnerId.Should().Be(registration.UserId));
        projects.Select(project => new { project.Name, project.Description }).Should().Equal(
            new { Name = "Apollo", Description = "Landing mission" },
            new { Name = "Orion", Description = "Deep space exploration" });
    }

    [Fact]
    public async Task ImportProject_ShouldReturn201_WhenJsonArrayIsValid()
    {
        var registration = await RegisterAndLoginAsync("import-project-json@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);
        const string json = """
            [
              { "name": "Website Redesign", "description": "Refresh the public site." },
              { "name": "Customer Portal", "description": "Create a self-service portal." }
            ]
            """;
        using var content = CreateImportContent("projects.json", json, "application/json");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ImportProjectResult>();
        result.Should().NotBeNull();
        result!.ProjectIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportProject_ShouldReturn422ProblemDetails_WhenCsvRowIsInvalid()
    {
        var registration = await RegisterAndLoginAsync("import-project-invalid-csv@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);
        using var content = CreateImportContent(
            "projects.csv",
            "\"Valid\",\"Row\"\r\nInvalid");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        await AssertImportErrorAsync(response, "Invalid Csv row at line 2.");
    }

    [Fact]
    public async Task ImportProject_ShouldReturn422ProblemDetails_WhenJsonIsMalformed()
    {
        var registration = await RegisterAndLoginAsync("import-project-invalid-json@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);
        const string json = """
            [
              { "name": "Valid", "description": "Row" },
              invalid
            ]
            """;
        using var content = CreateImportContent("projects.json", json, "application/json");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        await AssertImportErrorAsync(response, "Invalid JSON content at line 3.");
    }

    [Fact]
    public async Task ImportProject_ShouldReturn422_WhenFileExtensionIsUnsupported()
    {
        var registration = await RegisterAndLoginAsync("import-project-extension@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.Token);
        using var content = CreateImportContent("projects.txt", "\"Apollo\",\"Landing mission\"");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ImportProject_ShouldReturn401_WhenNotAuthenticated()
    {
        using var content = CreateImportContent("projects.csv", "\"Apollo\",\"Landing mission\"");

        var response = await _client.PostAsync(Paths.ProjectImport, content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent CreateImportContent(
        string fileName,
        string contents,
        string mediaType = "text/csv")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(contents));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        content.Add(fileContent, "file", fileName);

        return content;
    }

    private static async Task AssertImportErrorAsync(
        HttpResponseMessage response,
        string expectedDetail)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("title").GetString().Should().Be("Import validation failed");
        problem.RootElement.GetProperty("detail").GetString().Should().Be(expectedDetail);
        problem.RootElement.TryGetProperty("errors", out _).Should().BeFalse();
    }
    private async Task<RegisterUserResult> RegisterAndLoginAsync(
        string email,
        string password = "P@ssw0rd!")
    {
        var registerResponse = await _client.PostAsJsonAsync(
            Paths.Users,
            new RegisterUserCommand(email, password, "Test", "User", "Developer"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResult>();
        registration.Should().NotBeNull();

        var loginResponse = await _client.PostAsJsonAsync(Paths.Login, new LoginUserCommand(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginUserResult>();
        loginResult.Should().NotBeNull();

        return registration! with { Token = loginResult!.Token };
    }
}
