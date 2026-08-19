using System.Text;
using FluentAssertions;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Importers;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Tests.Unit.Importing.TaskItems;

public class JsonImporterTests
{
    [Theory]
    [InlineData(FileExtension.Json, true)]
    [InlineData(FileExtension.Csv, false)]
    [InlineData(FileExtension.Unknown, false)]
    public void CanImport_ShouldReturnWhetherTheExtensionIsJson(FileExtension fileExtension, bool expected)
    {
        var importer = new JsonImporter();

        importer.CanImport(fileExtension).Should().Be(expected);
    }

    [Fact]
    public async Task Import_ShouldDeserializeCaseInsensitiveProperties_AndIgnoreNullEntries()
    {
        const string json = """
            [
              { "title": "Design API", "description": "Define clear endpoints.", "status": "Todo" },
              null,
              { "TITLE": "Implement API", "DESCRIPTION": "Build the endpoints.", "STATUS": "Todo" }
            ]
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var importer = new JsonImporter();
        var rows = new List<TaskItemLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows
            .Select(row => 
                (row.Title.ToString(), row.Description.ToString(), row.Status.ToString()))
            .Should().Equal(
                ("Design API", "Define clear endpoints.", "Todo"),
                ("Implement API", "Build the endpoints.", "Todo"));
    }
}
