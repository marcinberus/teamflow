using System.Text;
using FluentAssertions;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Importerts;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Tests.Unit.Importing.Projects;

public class JsonSourceGenerationImporterTests
{
    [Theory]
    [InlineData(FileExtension.Json, true)]
    [InlineData(FileExtension.Csv, false)]
    [InlineData(FileExtension.Unknown, false)]
    public void CanImport_ShouldReturnWhetherTheExtensionIsJson(FileExtension fileExtension, bool expected)
    {
        var importer = new JsonSourceGenerationImporter();

        importer.CanImport(fileExtension).Should().Be(expected);
    }

    [Fact]
    public async Task Import_ShouldDeserializeCaseInsensitiveProperties_AndIgnoreNullEntries()
    {
        const string json = """
            [
              { "name": "Website Redesign", "description": "Refresh the public site." },
              null,
              { "NAME": "Mobile App", "DESCRIPTION": "Build the first release." }
            ]
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var importer = new JsonSourceGenerationImporter();
        var rows = new List<ProjectLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows.Should().Equal(
            new ProjectLine("Website Redesign", "Refresh the public site."),
            new ProjectLine("Mobile App", "Build the first release."));
    }

    [Fact]
    public async Task Import_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        var importer = new JsonSourceGenerationImporter();

        var action = async () =>
        {
            await foreach (var _ in importer.Import(null!, CancellationToken.None))
            {
            }
        };

        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }
}
