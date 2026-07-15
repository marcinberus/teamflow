using Dapper;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Infrastructure.Database.ReadServices;

public sealed class ProjectReadService(ISqlConnectionFactory connectionFactory) : IProjectReadService
{
    public async Task<ProjectDetailsDto?> GetProjectByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.Id,
                p.Name,
                p.Description,
                p.Status,
                p.OwnerId,
                CONCAT(u.FirstName, ' ', u.LastName) AS OwnerName,
                p.CreatedAt,
                p.UpdatedAt
            FROM Projects p
            INNER JOIN Users u ON u.Id = p.OwnerId
            WHERE p.Id = @ProjectId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ProjectDetailsDto>(command);
    }

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

    public async Task<IReadOnlyList<ProjectMemberDto>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                pm.Id AS MemberId,
                u.Id AS UserId,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Role,
                pm.Role AS ProjectRole,
                pm.CreatedAt AS JoinedAt
            FROM ProjectMembers pm
            INNER JOIN Users u ON u.Id = pm.UserId
            WHERE pm.ProjectId = @ProjectId
            ORDER BY pm.CreatedAt, pm.Id;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { ProjectId = projectId },
            cancellationToken: cancellationToken);

        var members = await connection.QueryAsync<ProjectMemberDto>(command);

        return members.AsList();
    }
}
