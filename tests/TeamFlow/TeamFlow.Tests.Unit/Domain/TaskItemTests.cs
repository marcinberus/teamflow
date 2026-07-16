using FluentAssertions;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Domain;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Now = new();

    [Fact]
    public void TaskItem_ChangeStatus_ShouldUpdateStatus()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "Design API", "Details", null, null, Now);
        var later = Now.AddHours(1);

        task.ChangeStatus(TaskItemStatus.InProgress, later);

        task.Status.Should().Be(TaskItemStatus.InProgress);
        task.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void TaskItem_Update_ShouldUpdateEditableFields()
    {
        var assigneeId = Guid.NewGuid();
        var dueDate = Now.AddDays(2);
        var later = Now.AddHours(1);
        var task = TaskItem.Create(Guid.NewGuid(), "Design API", "Details", null, null, Now);

        task.Update("Design REST API", "Updated details", assigneeId, dueDate, later);

        task.Title.Should().Be("Design REST API");
        task.Description.Should().Be("Updated details");
        task.AssignedUserId.Should().Be(assigneeId);
        task.DueDate.Should().Be(dueDate);
        task.UpdatedAt.Should().Be(later);
    }
}
