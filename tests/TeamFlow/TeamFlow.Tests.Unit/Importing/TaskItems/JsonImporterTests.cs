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
              { "title": "Design API", "description": "Define clear endpoints.", "userId": "user-1", "dueDate": "2027-05-20T10:00:00+00:00", "status": "Todo" },
              null,
              { "TITLE": "Implement API", "DESCRIPTION": "Build the endpoints.", "USERID": "user-2", "DUEDATE": "2027-06-21T11:30:00+00:00", "STATUS": "Todo" }
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
                (row.Title.ToString(), row.Description.ToString(), row.UserId.ToString(), row.DueDate.ToString(), row.Status.ToString()))
            .Should().Equal(
                ("Design API", "Define clear endpoints.", "user-1", "2027-05-20T10:00:00+00:00", "Todo"),
                ("Implement API", "Build the endpoints.", "user-2", "2027-06-21T11:30:00+00:00", "Todo"));
    }
}
