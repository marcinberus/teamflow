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
        var separatorValue = Separator.AsSpan();
        var separatorIndex = lineValue.IndexOf(separatorValue);

        if (lineValue.Length < 5
            || lineValue[0] != Quote
            || lineValue[^1] != Quote
            || separatorIndex < 1)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        var startIndex = 1;
        var endIndex = separatorIndex;
        var title = line[startIndex..endIndex];


        startIndex = endIndex + separatorValue.Length;
        var remainingLine = line[startIndex..^1].Span;
        var offset = remainingLine.IndexOf(separatorValue);

        if (offset < 0)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        endIndex = startIndex + offset;
        var description = line[startIndex..endIndex];


        startIndex = endIndex + separatorValue.Length;
        remainingLine = line[startIndex..^1].Span;
        offset = remainingLine.IndexOf(separatorValue);

        if (offset < 0)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        endIndex = startIndex + offset;
        var userId = line[startIndex..endIndex];


        startIndex = endIndex + separatorValue.Length;
        var status = line[startIndex..^1];

        if (title.Span.IndexOf(Quote) >= 0 
            || description.Span.IndexOf(Quote) >= 0 
            || status.Span.IndexOf(Quote) >= 0)
        {
            throw CreateInvalidRowException(lineNumber);
        }

        return new TaskItemLine(
            title,
            description,
            userId,
            status);
    }

    private static FormatException CreateInvalidRowException(int lineNumber)
    {
        return ImportExceptions.CreateInvalidRowException(FileExtension.Csv.ToString(), lineNumber);
    }
}
