using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Projects.Queries.GetProjectStatistics;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class GetProjectStatisticsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnStatistics_WhenProjectExists()
    {
        var readService = Substitute.For<IProjectReadService>();
        var projectId = Guid.NewGuid();
        var statistics = new ProjectStatisticsDto(
            projectId,
            3,
            new Dictionary<string, int>
            {
                ["Todo"] = 1,
                ["InProgress"] = 1,
                ["Verification"] = 0,
                ["Done"] = 1,
                ["Cancelled"] = 0
            },
            2,
            "33.33");
        readService.GetStatisticsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(statistics);
        var handler = new GetProjectStatisticsQueryHandler(readService);

        var result = await handler.Handle(
            new GetProjectStatisticsQuery(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(statistics);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundFailure_WhenProjectDoesNotExist()
    {
        var readService = Substitute.For<IProjectReadService>();
        var projectId = Guid.NewGuid();
        readService.GetStatisticsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsDto?)null);
        var handler = new GetProjectStatisticsQueryHandler(readService);

        var result = await handler.Handle(
            new GetProjectStatisticsQuery(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.NotFound);
    }
}
