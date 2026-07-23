using System.Runtime.CompilerServices;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects.Importerts;

public class CsvImporter : IProjectImporter
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
        var row = line.AsSpan();
        var separatorIndex = row.IndexOf(Separator.AsSpan());

        if (row.Length < 5
            || row[0] != Quote
            || row[^1] != Quote
            || separatorIndex < 1
            || separatorIndex + 3 > row.Length - 1)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        var name = row[1..separatorIndex];
        var description = row[(separatorIndex + 3)..^1];

        if (name.IndexOf(Quote) >= 0 || description.IndexOf(Quote) >= 0)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        return new ProjectLine(
            name.ToString(),
            description.ToString());
    }

    private static FormatException CreateInvalidRowException(int lineNumber)
    {
        return new FormatException(
            $"Invalid CSV row at line {lineNumber}.");
    }
}
