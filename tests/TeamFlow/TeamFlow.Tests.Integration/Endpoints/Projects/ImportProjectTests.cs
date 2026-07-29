using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

    private static MultipartFormDataContent CreateImportContent(string fileName, string csv)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);

        return content;
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
