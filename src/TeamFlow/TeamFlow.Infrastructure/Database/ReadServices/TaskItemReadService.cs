using Dapper;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Tasks.DTOs;
using TeamFlow.Application.Tasks.Interfaces;

namespace TeamFlow.Infrastructure.Database.ReadServices;

public sealed class TaskItemReadService(ISqlConnectionFactory connectionFactory) : ITaskItemReadService
{
    public async Task<(IReadOnlyList<TaskItemDto> Items, int TotalCount)> ListTasksAsync(
        Guid projectId,
        string? status,
        Guid? assignedUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.AssignedUserId,
                CASE
                    WHEN u.Id IS NULL THEN NULL
                    ELSE CONCAT(u.FirstName, ' ', u.LastName)
                END AS AssigneeName,
                t.DueDate,
                t.CreatedAt
            FROM Tasks t
            LEFT JOIN Users u ON u.Id = t.AssignedUserId
            WHERE t.ProjectId = @ProjectId
                AND (@Status IS NULL OR t.Status = @Status)
                AND (@AssignedUserId IS NULL OR t.AssignedUserId = @AssignedUserId)
            ORDER BY t.CreatedAt DESC, t.Id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*)
            FROM Tasks t
            WHERE t.ProjectId = @ProjectId
                AND (@Status IS NULL OR t.Status = @Status)
                AND (@AssignedUserId IS NULL OR t.AssignedUserId = @AssignedUserId);
            """;

        var parameters = new
        {
            ProjectId = projectId,
            Status = string.IsNullOrWhiteSpace(status) ? null : status,
            AssignedUserId = assignedUserId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);

        var items = (await results.ReadAsync<TaskItemDto>()).AsList();
        var totalCount = await results.ReadSingleAsync<int>();

        return (items, totalCount);
    }
}
