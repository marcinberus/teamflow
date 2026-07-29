using FluentAssertions;
using NSubstitute;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Tests.Unit.Importing;

public class ProjectImportManagerTests
{
    [Fact]
    public async Task Import_ShouldDelegateToMatchingImporter_WhenImporterSupportsTheFileExtension()
    {
        var matchingImporter = Substitute.For<IProjectImporter>();
        var otherImporter = Substitute.For<IProjectImporter>();
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var expectedLines = new[]
        {
            new ProjectLine("Website redesign", "Refresh the public site"),
            new ProjectLine("Mobile app", "Build the customer app")
        };

        matchingImporter.CanImport(FileExtension.Csv).Returns(true);
        matchingImporter.Import(stream, cancellationToken)
            .Returns(CreateAsyncEnumerable(expectedLines));
        var manager = new ProjectImportManager([matchingImporter, otherImporter]);
        var lines = new List<ProjectLine>();

        await foreach (var line in manager.Import(FileExtension.Csv, stream, cancellationToken))
        {
            lines.Add(line);
        }

        lines.Should().Equal(expectedLines);
        matchingImporter.Received(1).Import(stream, cancellationToken);
        otherImporter.Received(1).CanImport(FileExtension.Csv);
    }

    [Fact]
    public async Task Import_ShouldReturnNoLines_WhenNoImporterSupportsTheFileExtension()
    {
        var csvImporter = Substitute.For<IProjectImporter>();
        var jsonImporter = Substitute.For<IProjectImporter>();
        using var stream = new MemoryStream();
        var manager = new ProjectImportManager([csvImporter, jsonImporter]);
        var lines = new List<ProjectLine>();

        await foreach (var line in manager.Import(FileExtension.Unknown, stream, CancellationToken.None))
        {
            lines.Add(line);
        }

        lines.Should().BeEmpty();
        csvImporter.DidNotReceive().Import(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        jsonImporter.DidNotReceive().Import(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Import_ShouldFail_WhenMoreThanOneImporterSupportsTheFileExtension()
    {
        var firstImporter = Substitute.For<IProjectImporter>();
        var secondImporter = Substitute.For<IProjectImporter>();
        using var stream = new MemoryStream();

        firstImporter.CanImport(FileExtension.Csv).Returns(true);
        secondImporter.CanImport(FileExtension.Csv).Returns(true);
        var manager = new ProjectImportManager([firstImporter, secondImporter]);

        var action = () => manager.Import(FileExtension.Csv, stream, CancellationToken.None);

        action.Should().Throw<InvalidOperationException>();
        firstImporter.DidNotReceive().Import(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        secondImporter.DidNotReceive().Import(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<ProjectLine> CreateAsyncEnumerable(IEnumerable<ProjectLine> lines)
    {
        await Task.CompletedTask;

        foreach (var line in lines)
        {
            yield return line;
        }
    }
}
