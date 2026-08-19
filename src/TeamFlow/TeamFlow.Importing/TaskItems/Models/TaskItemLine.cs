namespace TeamFlow.Importing.TaskItems.Models;

public record TaskItemLine(
    ReadOnlyMemory<char> Title,
    ReadOnlyMemory<char> Description,
    ReadOnlyMemory<char> UserId,
    ReadOnlyMemory<char> Status) : IImportLine;
