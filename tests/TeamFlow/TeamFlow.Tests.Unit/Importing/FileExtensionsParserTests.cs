using FluentAssertions;
using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Tests.Unit.Importing;

public class FileExtensionsParserTests
{
    [Theory]
    [InlineData(".csv", FileExtension.Csv)]
    [InlineData(".CSV", FileExtension.Csv)]
    [InlineData(".json", FileExtension.Json)]
    public void TryParse_ShouldParseKnownExtension(string input, FileExtension expected)
    {
        var result = FileExtensionParser.TryParse(input, out var extension);

        result.Should().BeTrue();
        extension.Should().Be(expected);
    }

    [Theory]
    [InlineData(".csvv", FileExtension.Unknown)]
    [InlineData(".exe", FileExtension.Unknown)]
    [InlineData("", FileExtension.Unknown)]
    [InlineData(null, FileExtension.Unknown)]
    public void TryParse_ShouldReturnUnknownExtension(string? input, FileExtension expected)
    {
        var result = FileExtensionParser.TryParse(input, out var extension);

        result.Should().BeFalse();
        extension.Should().Be(expected);
    }
}
