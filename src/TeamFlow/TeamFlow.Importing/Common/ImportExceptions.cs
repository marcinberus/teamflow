using System.Text.Json;

namespace TeamFlow.Importing.Common;

public static class ImportExceptions
{
    public static FormatException CreateInvalidRowException(string format, int lineNumber)
        => new(ErrorMessages.InvalidRow(format, lineNumber));

    public static FormatException CreateInvalidJsonException(JsonException exception)
    {
        var lineNumber = exception.LineNumber + 1;
        return new FormatException(ErrorMessages.InvalidJson(lineNumber), exception);
    }
}
