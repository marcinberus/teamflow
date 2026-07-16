namespace TeamFlow.Application.Projects.DTOs;

public record ProjectStatisticsDto(
    Guid ProjectId,
    int TotalTasks,
    Dictionary<string, int> TasksByStatus,
    int TotalMembers,
    string CompletionPercentage);
