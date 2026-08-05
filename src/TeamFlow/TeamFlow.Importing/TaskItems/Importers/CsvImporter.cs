using System.Runtime.CompilerServices;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Importing.TaskItems.Importers;

public class CsvImporter : ITaskItemImporter
{
    private const string Separator = "\",\"";
    private const char Quote = '"';

    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Csv;

    public async IAsyncEnumerable<TaskItemLine> Import(
        Stream stream, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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

            yield return ParseLine(line.AsMemory(), lineNumber);
        }
    }

    private static TaskItemLine ParseLine(ReadOnlyMemory<char> line, int lineNumber)
    {
        var lineValue = line.Span;
        var separatorIndex = lineValue.IndexOf(Separator.AsSpan());

        if (lineValue.Length < 5
            || lineValue[0] != Quote
            || lineValue[^1] != Quote
            || separatorIndex < 1
            || separatorIndex + Separator.Length > lineValue.Length - 1)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        var title = line[1..separatorIndex];
        var description = line[(separatorIndex + Separator.Length)..^1];

        if (title.Span.IndexOf(Quote) >= 0 || description.Span.IndexOf(Quote) >= 0)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        return new TaskItemLine(
            title,
            description);
    }

    private static FormatException CreateInvalidRowException(int lineNumber)
    {
        return ImportExceptions.CreateInvalidRowException(FileExtension.Csv.ToString(), lineNumber);
    }
}
