using System.Runtime.CompilerServices;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects.Importerts;

public class CsvSplitImporter : IProjectImporter
{
    private const string Separator = "\",\"";
    private const char Quote = '"';

    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Csv;

    public async IAsyncEnumerable<ProjectLine> Import(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(ErrorMessages.StreamUnreadable);
        }

        using var reader = new StreamReader(stream);
        var lineNumber = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            yield return ParseLine(line, lineNumber);
        }
    }

    private static ProjectLine ParseLine(string line, int lineNumber)
    {
        var values = line.Split(Separator, StringSplitOptions.None);

        if (values.Length != 2
            || values[0].Length < 1
            || values[1].Length < 1
            || values[0][0] != Quote
            || values[1][^1] != Quote)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        var name = values[0][1..];
        var description = values[1][..^1];

        if (name.Contains(Quote) || description.Contains(Quote))
        {
            throw CreateInvalidRowException(lineNumber);
        }

        return new ProjectLine(name, description);
    }

    private static FormatException CreateInvalidRowException(int lineNumber)
    {
        return new FormatException(
            $"Invalid CSV row at line {lineNumber}.");
    }
}
