namespace TeamFlow.Importing.Common;

internal class ErrorMessages
{
    public const string StreamUnreadable = "Stream doesn't handle read.";

    public static string InvalidRow(string format, int lineNumber) 
        => $"Invalid {format} row at line {lineNumber}.";
}
