using System.Text;
using FluentAssertions;
using TeamFlow.Importing.TaskItems.Importers;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Tests.Unit.Importing.TaskItems;

public class CsvImporterTests
{
    [Fact]
    public async Task Import_ShouldParseRows_WhenFieldsAreSurroundedByQuotes()
    {
        const string csv = "\"Design, API\",\"Define clear, accessible endpoints.\",\"Todo\"\r\n"
            + "\"Plain title\",\"Plain description\",\"InProgress\"";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();
        var rows = new List<TaskItemLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows.Select(row => 
            (row.Title.ToString(), row.Description.ToString(), row.Status.ToString()))
                .Should().Equal(
            ("Design, API", "Define clear, accessible endpoints.", "Todo"),
            ("Plain title", "Plain description", "InProgress"));
    }

    [Theory]
    [InlineData("Design API,Description")]
    [InlineData("\"Design API\",Description")]
    [InlineData("Design API,\"Description\"")]
    [InlineData("\"Design API\",\"Description\",\"Todo\",\"Extra\"")]
    public async Task Import_ShouldFail_WhenFieldsAreNotSurroundedByQuotes(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();

        var action = async () =>
        {
            await foreach (var _ in importer.Import(stream, CancellationToken.None))
            {
            }
        };

        await action.Should()
            .ThrowAsync<FormatException>()
            .WithMessage("*line 1*");
    }

    [Theory]
    [InlineData("\"Design API\"More\",\"Description\"")]
    [InlineData("\"Design API\",\"Description\"More\"")]
    public async Task Import_ShouldFail_WhenFieldContainsAdditionalQuote(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();

        var action = async () =>
        {
            await foreach (var _ in importer.Import(stream, CancellationToken.None))
            {
            }
        };

        await action.Should()
            .ThrowAsync<FormatException>()
            .WithMessage("*line 1*");
    }

    [Fact]
    public async Task Import_ShouldIgnoreBlankLines_AndReportTheOriginalLineNumber()
    {
        const string csv = "\r\n\"Valid\",\"Row\",\"Todo\"\r\n\r\nInvalid";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();
        var rows = new List<TaskItemLine>();

        var action = async () =>
        {
            await foreach (var row in importer.Import(stream, CancellationToken.None))
            {
                rows.Add(row);
            }
        };

        await action.Should()
            .ThrowAsync<FormatException>()
            .WithMessage("*line 4*");
        rows.Should().ContainSingle();
        rows[0].Title.ToString().Should().Be("Valid");
        rows[0].Description.ToString().Should().Be("Row");
    }
}
