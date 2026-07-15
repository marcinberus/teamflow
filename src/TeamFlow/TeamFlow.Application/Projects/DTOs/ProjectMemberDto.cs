namespace TeamFlow.Application.Projects.DTOs;

public sealed record ProjectMemberDto(
    Guid MemberId,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string ProjectRole,
    DateTimeOffset JoinedAt);
