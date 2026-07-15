using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Tasks.DTOs;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Application.Tasks.Queries.ListTasks;

namespace TeamFlow.Tests.Unit.Application.Tasks.Queries;

public sealed class ListTasksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPaginatedTasks_FromReadService()
    {
        var projectId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var readService = Substitute.For<ITaskItemReadService>();
        var item = new TaskItemDto(
            Guid.NewGuid(),
            "Design API",
            "Define endpoints",
            "InProgress",
            assignedUserId,
            "Alice Smith",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow);
        IReadOnlyList<TaskItemDto> items = [item];

        readService.ListTasksAsync(
                projectId,
                "InProgress",
                assignedUserId,
                2,
                10,
                Arg.Any<CancellationToken>())
            .Returns((items, 11));

        var handler = new ListTasksQueryHandler(readService);

        var result = await handler.Handle(
            new ListTasksQuery(projectId, "InProgress", assignedUserId, 2, 10),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().ContainSingle().Which.Should().Be(item);
        result.Value.TotalCount.Should().Be(11);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
    }
}
