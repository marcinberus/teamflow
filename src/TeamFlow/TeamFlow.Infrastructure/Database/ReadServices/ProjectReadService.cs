using Dapper;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Infrastructure.Database.ReadServices;

public sealed class ProjectReadService(ISqlConnectionFactory connectionFactory) : IProjectReadService
{
    public async Task<(IReadOnlyList<ProjectSummaryDto> Items, int TotalCount)> ListProjectsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.Id,
                p.Name,
                p.Description,
                p.Status,
                CONCAT(u.FirstName, ' ', u.LastName) AS OwnerName,
                (SELECT COUNT(*) FROM Tasks t WHERE t.ProjectId = p.Id) AS TaskCount,
                (SELECT COUNT(*) FROM ProjectMembers pm WHERE pm.ProjectId = p.Id) AS MemberCount,
                p.CreatedAt
            FROM Projects p
            INNER JOIN Users u ON u.Id = p.OwnerId
            WHERE (@Status IS NULL OR p.Status = @Status)
            ORDER BY p.CreatedAt DESC, p.Id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*)
            FROM Projects p
            WHERE (@Status IS NULL OR p.Status = @Status);
            """;

        var parameters = new
        {
            Status = string.IsNullOrWhiteSpace(status) ? null : status,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);

        var items = (await results.ReadAsync<ProjectSummaryDto>()).AsList();
        var totalCount = await results.ReadSingleAsync<int>();

        return (items, totalCount);
    }
}
