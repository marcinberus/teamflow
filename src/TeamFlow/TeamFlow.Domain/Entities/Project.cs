using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Domain.Entities;

public sealed class Project : Entity
{
    private readonly List<ProjectMember> _members = [];
    private readonly List<TaskItem> _tasks = [];

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public ProjectStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }

    public IReadOnlyList<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyList<TaskItem> Tasks => _tasks.AsReadOnly();

    private Project() 
    {
    }

    public static Project Create(string name, string description, Guid ownerId, DateTimeOffset now)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Status = ProjectStatus.Active,
            OwnerId = ownerId,
            CreatedAt = now
        };
    }

    public void AssignMember(Guid userId, Role role, DateTimeOffset now)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new ConflictException($"User {userId} is already a member of this project.");
        }

        _members.Add(ProjectMember.Create(Id, userId, role, now));
    }

    public bool CanAssignMembers(Guid userId, Role? userRole)
    {
        return OwnerId == userId
            || userRole is Role.Manager or Role.Admin;
    }

    public bool HasMember(Guid userId)
    {
        return OwnerId == userId || _members.Any(member => member.UserId == userId);
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
        {
            throw new ConflictException("Cannot remove the project owner from a project.");
        }

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException($"User {userId} is not a member of this project.");

        _members.Remove(member);
    }

    public void ChangeStatus(ProjectStatus newStatus, DateTimeOffset updatedAt)
    {
        Status = newStatus;
        UpdatedAt = updatedAt;
    }

    public void Update(string name, string description, DateTimeOffset updatedAt)
    {
        Name = name;
        Description = description;
        UpdatedAt = updatedAt;
    }

    public TaskItem AddTask(
        string title,
        string description,
        Guid? assignedUserId,
        DateTimeOffset? dueDate,
        DateTimeOffset now)
    {
        var task = TaskItem.Create(Id, title, description, assignedUserId, dueDate, now);
        _tasks.Add(task);
        return task;
    }
}
