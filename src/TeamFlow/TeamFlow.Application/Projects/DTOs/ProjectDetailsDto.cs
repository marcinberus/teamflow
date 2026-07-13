namespace TeamFlow.Application.Projects.DTOs;

public record ProjectDetailsDto(
    Guid Id,
    string Name,
    string Description,
    string Status,
    Guid OwnerId,
    string OwnerName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
