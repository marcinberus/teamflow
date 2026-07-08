using FluentAssertions;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Tests.Unit.Domain;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset Now = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Project_AssignMember_ShouldAddMember_WhenNotAlreadyAssigned()
    {
        var project = Project.Create("Apollo", "Moon landing", Guid.NewGuid(), Now);
        var userId = Guid.NewGuid();

        project.AssignMember(userId, Role.Developer, Now);

        project.Members.Should().ContainSingle(m => m.UserId == userId);
    }

    [Fact]
    public void Project_AssignMember_ShouldThrow_WhenAlreadyAssigned()
    {
        var project = Project.Create("Apollo", "Moon landing", Guid.NewGuid(), Now);
        var userId = Guid.NewGuid();
        project.AssignMember(userId, Role.Developer, Now);

        var act = () => project.AssignMember(userId, Role.Developer, Now);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void Project_RemoveMember_ShouldThrow_WhenMemberNotFound()
    {
        var project = Project.Create("Apollo", "Moon landing", Guid.NewGuid(), Now);

        var act = () => project.RemoveMember(Guid.NewGuid());

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void Project_ChangeStatus_ShouldUpdateStatusAndTimestamp()
    {
        var project = Project.Create("Apollo", "Moon landing", Guid.NewGuid(), Now);
        var later = Now.AddHours(1);

        project.ChangeStatus(ProjectStatus.OnHold, later);

        project.Status.Should().Be(ProjectStatus.OnHold);
        project.UpdatedAt.Should().Be(later);
    }
}
