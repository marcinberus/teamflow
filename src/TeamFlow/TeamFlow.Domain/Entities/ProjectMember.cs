using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class ProjectMember : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }

    private ProjectMember()
    {
    }

    public static ProjectMember Create(Guid projectId, Guid userId, Role role, DateTimeOffset now)
    {
        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            CreatedAt = now
        };
    }
}
