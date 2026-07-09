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
}
