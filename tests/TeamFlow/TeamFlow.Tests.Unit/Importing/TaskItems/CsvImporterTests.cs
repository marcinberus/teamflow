using System.Text;
using FluentAssertions;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Importers;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Tests.Unit.Importing.TaskItems;

public class CsvImporterTests
{
    [Theory]
    [InlineData(FileExtension.Csv, true)]
    [InlineData(FileExtension.Json, false)]
    [InlineData(FileExtension.Unknown, false)]
    public void CanImport_ShouldReturnWhetherTheExtensionIsCsv(FileExtension fileExtension, bool expected)
    {
        var importer = new CsvImporter();

        importer.CanImport(fileExtension).Should().Be(expected);
    }

    [Fact]
    public async Task Import_ShouldParseRows_WhenFieldsAreSurroundedByQuotes()
    {
        const string csv = "\"Design, API\",\"Define clear, accessible endpoints.\",\"user-1\",\"2027-05-20T10:00:00+00:00\",\"Todo\"\r\n"
            + "\"Plain title\",\"Plain description\",\"user-2\",\"2027-06-21T11:30:00+00:00\",\"InProgress\"";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();
        var rows = new List<TaskItemLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows.Select(row =>
            (row.Title.ToString(), row.Description.ToString(), row.UserId.ToString(), row.DueDate.ToString(), row.Status.ToString()))
            .Should().Equal(
                ("Design, API", "Define clear, accessible endpoints.", "user-1", "2027-05-20T10:00:00+00:00", "Todo"),
                ("Plain title", "Plain description", "user-2", "2027-06-21T11:30:00+00:00", "InProgress"));
    }

    [Theory]
    [InlineData("Design API,Description")]
    [InlineData("\"Design API\",Description")]
    [InlineData("Design API,\"Description\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\",\"2027-05-20\",\"Todo\",\"Extra\"")]
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
    [InlineData("\"Design API\"More\",\"Description\",\"user-1\",\"2027-05-20\",\"Todo\"")]
    [InlineData("\"Design API\",\"Description\"More\",\"user-1\",\"2027-05-20\",\"Todo\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\"More\",\"2027-05-20\",\"Todo\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\",\"2027-05-20\"More\",\"Todo\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\",\"2027-05-20\",\"Todo\"More\"")]
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

    [Theory]
    [InlineData("\"Design API\",\"Description\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\"")]
    [InlineData("\"Design API\",\"Description\",\"user-1\",\"2027-05-20\"")]
    public async Task Import_ShouldFail_WhenRequiredFieldIsMissing(string csv)
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
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public async Task Import_ShouldReturnNoRows_WhenInputContainsOnlyBlankLines(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvImporter();
        var rows = new List<TaskItemLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_ShouldFail_WhenStreamIsUnreadable()
    {
        using var stream = new MemoryStream();
        stream.Close();
        var importer = new CsvImporter();

        var action = async () =>
        {
            await foreach (var _ in importer.Import(stream, CancellationToken.None))
            {
            }
        };

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Stream doesn't handle read.");
    }

    [Fact]
    public async Task Import_ShouldIgnoreBlankLines_AndReportTheOriginalLineNumber()
    {
        const string csv = "\r\n\"Valid\",\"Row\",\"user-1\",\"2027-05-20\",\"Todo\"\r\n\r\nInvalid";

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
        rows[0].UserId.ToString().Should().Be("user-1");
        rows[0].DueDate.ToString().Should().Be("2027-05-20");
    }
}
