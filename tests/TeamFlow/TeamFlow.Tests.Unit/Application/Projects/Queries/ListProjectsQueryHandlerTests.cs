using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Projects.Queries.ListProjects;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class ListProjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPaginatedProjects_FromReadService()
    {
        var readService = Substitute.For<IProjectReadService>();
        var item = new ProjectSummaryDto(
            Guid.NewGuid(),
            "Apollo",
            "Landing mission",
            "Active",
            "Alice Smith",
            5,
            3,
            DateTimeOffset.UtcNow);
        IReadOnlyList<ProjectSummaryDto> items = [item];

        readService.ListProjectsAsync(2, 10, "Active", Arg.Any<CancellationToken>())
            .Returns((items, 15));

        var handler = new ListProjectsQueryHandler(readService);

        var result = await handler.Handle(new ListProjectsQuery(2, 10, "Active"), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Should().Be(item);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }
}
