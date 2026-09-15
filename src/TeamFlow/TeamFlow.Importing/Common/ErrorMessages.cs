namespace TeamFlow.Importing.Common;

internal class ErrorMessages
{
    public const string StreamUnreadable = "Stream doesn't handle read.";

    public static string InvalidRow(string format, long lineNumber)
        => $"Invalid {format} row at line {lineNumber}.";

    public static string InvalidJson(long? lineNumber)
        => lineNumber is null ? "Invalid JSON content." : $"Invalid JSON content at line {lineNumber}.";
}
