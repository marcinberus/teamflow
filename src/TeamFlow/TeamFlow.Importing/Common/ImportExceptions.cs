namespace TeamFlow.Importing.Common;

public static class ImportExceptions
{
    public static FormatException CreateInvalidRowException(string format, int lineNumber)
        => new FormatException(ErrorMessages.InvalidRow(format, lineNumber));
}
