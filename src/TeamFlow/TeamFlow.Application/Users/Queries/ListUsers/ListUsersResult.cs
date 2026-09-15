using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Application.Users.Queries.ListUsers;

public record ListUsersResult(
    IReadOnlyList<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
