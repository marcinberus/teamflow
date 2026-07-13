namespace TeamFlow.Application.Projects.DTOs;

public record ProjectSummaryDto(
    Guid Id,
    string Name,
    string Description,
    string Status,
    string OwnerName,
    int TaskCount,
    int MemberCount,
    DateTimeOffset CreatedAt);
