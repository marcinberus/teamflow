namespace TeamFlow.Application.Tasks.DTOs;

public record TaskItemDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    Guid? AssignedUserId,
    string? AssigneeName,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt);
