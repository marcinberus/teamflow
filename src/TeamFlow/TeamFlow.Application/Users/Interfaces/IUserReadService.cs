using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Application.Users.Interfaces;

public interface IUserReadService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<UserSummaryDto> Items, int TotalCount)> ListUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
