using TeamFlow.Application.Tasks.DTOs;

namespace TeamFlow.Application.Tasks.Queries.ListTasks;

public record ListTasksResult(
    IReadOnlyList<TaskItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
