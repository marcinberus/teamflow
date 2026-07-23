using System.Text;
using FluentAssertions;
using TeamFlow.Importing.Projects.Importerts;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Tests.Unit.Importing;

public class CsvSplitImporterTests
{
    [Fact]
    public async Task Import_ShouldParseRows_WhenFieldsAreSurroundedByQuotes()
    {
        const string csv = "\"Website, Redesign\",\"A clear, accessible site.\"\r\n"
            + "\"Plain name\",\"Plain description\"";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvSplitImporter();
        var rows = new List<ProjectLine>();

        await foreach (var row in importer.Import(stream, CancellationToken.None))
        {
            rows.Add(row);
        }

        rows.Should().Equal(
            new ProjectLine("Website, Redesign", "A clear, accessible site."),
            new ProjectLine("Plain name", "Plain description"));
    }

    [Theory]
    [InlineData("Website,Description")]
    [InlineData("\"Website\",Description")]
    [InlineData("Website,\"Description\"")]
    [InlineData("\"Website\",\"Description\",\"Extra\"")]
    public async Task Import_ShouldFail_WhenCsvRowIsInvalid(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvSplitImporter();

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
    [InlineData("\"Website\"More\",\"Description\"")]
    [InlineData("\"Website\",\"Description\"More\"")]
    public async Task Import_ShouldFail_WhenFieldContainsAdditionalQuote(string csv)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvSplitImporter();

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
        const string csv = "\r\n\"Valid\",\"Row\"\r\n\r\nInvalid";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importer = new CsvSplitImporter();
        var rows = new List<ProjectLine>();

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
        rows.Should().ContainSingle()
            .Which.Should().Be(new ProjectLine("Valid", "Row"));
    }
}
