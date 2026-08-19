using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class TaskItem : Entity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public TaskItemStatus Status { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private TaskItem() 
    {
    }

    public static TaskItem Create(
        Guid projectId,
        string title,
        string description,
        Guid? assignedUserId,
        DateTimeOffset? dueDate,
        DateTimeOffset now,
        TaskItemStatus status = TaskItemStatus.Todo)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Description = description,
            Status = status,
            AssignedUserId = assignedUserId,
            DueDate = dueDate,
            CreatedAt = now
        };
    }

    public void ChangeStatus(TaskItemStatus newStatus, DateTimeOffset updatedAt)
    {
        Status = newStatus;
        UpdatedAt = updatedAt;
    }

    public void Update(
        string title,
        string description,
        Guid? assignedUserId,
        DateTimeOffset? dueDate,
        DateTimeOffset updatedAt)
    {
        Title = title;
        Description = description;
        AssignedUserId = assignedUserId;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }
}
