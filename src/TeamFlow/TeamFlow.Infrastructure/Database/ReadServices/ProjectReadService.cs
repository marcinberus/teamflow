using System.Globalization;
using Dapper;
using TeamFlow.Domain.Enums;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Infrastructure.Database.ReadServices;

public sealed class ProjectReadService(ISqlConnectionFactory connectionFactory) : IProjectReadService
{
    public async Task<ProjectStatisticsDto?> GetStatisticsAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id
            FROM Projects
            WHERE Id = @ProjectId;

            SELECT Status, COUNT(*) AS Count
            FROM Tasks
            WHERE ProjectId = @ProjectId
            GROUP BY Status;

            SELECT COUNT(*)
            FROM ProjectMembers
            WHERE ProjectId = @ProjectId;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { ProjectId = projectId },
            cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);

        var existingProjectId = await results.ReadSingleOrDefaultAsync<Guid>();
        var taskStatusCounts = await results.ReadAsync<TaskStatusCount>();
        var totalMembers = await results.ReadSingleAsync<int>();

        if (existingProjectId == Guid.Empty)
        {
            return null;
        }

        var tasksByStatus = Enum.GetNames<TaskItemStatus>()
            .ToDictionary(status => status, _ => 0, StringComparer.Ordinal);

        foreach (var statusCount in taskStatusCounts)
        {
            tasksByStatus[statusCount.Status] = statusCount.Count;
        }

        var totalTasks = tasksByStatus.Values.Sum();
        var doneTasks = tasksByStatus.GetValueOrDefault("Done");
        var completionPercentage = totalTasks == 0
            ? 0d
            : (double)doneTasks / totalTasks * 100;
        var formattedCompletionPercentage = completionPercentage.ToString("F2", CultureInfo.InvariantCulture);

        return new ProjectStatisticsDto(
            existingProjectId,
            totalTasks,
            tasksByStatus,
            totalMembers,
            formattedCompletionPercentage);
    }

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

    private sealed class TaskStatusCount
    {
        public string Status { get; init; } = string.Empty;

        public int Count { get; init; }
    }
}
