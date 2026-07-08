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
}
