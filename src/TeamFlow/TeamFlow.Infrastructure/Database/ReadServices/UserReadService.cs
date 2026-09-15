using Dapper;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Infrastructure.Database.ReadServices;

public sealed class UserReadService : IUserReadService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserReadService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Email, FirstName, LastName, Role, CreatedAt
            FROM Users
            WHERE Id = @UserId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UserProfileDto>(sql, new { UserId = userId });
    }

    public async Task<(IReadOnlyList<UserSummaryDto> Items, int TotalCount)> ListUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Email, FirstName, LastName, Role
            FROM Users
            WHERE (@Search IS NULL
                   OR CHARINDEX(@Search, Email) > 0
                   OR CHARINDEX(@Search, FirstName) > 0
                   OR CHARINDEX(@Search, LastName) > 0
                   OR CHARINDEX(@Search, CONCAT(FirstName, ' ', LastName)) > 0)
            ORDER BY LastName, FirstName, Id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*)
            FROM Users
            WHERE (@Search IS NULL
                   OR CHARINDEX(@Search, Email) > 0
                   OR CHARINDEX(@Search, FirstName) > 0
                   OR CHARINDEX(@Search, LastName) > 0
                   OR CHARINDEX(@Search, CONCAT(FirstName, ' ', LastName)) > 0);
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);

        var items = (await results.ReadAsync<UserSummaryDto>()).AsList();
        var totalCount = await results.ReadSingleAsync<int>();

        return (items, totalCount);
    }
}