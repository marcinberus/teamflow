using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Projects.Queries.GetProject;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class GetProjectQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnProject_WhenProjectExists()
    {
        var readService = Substitute.For<IProjectReadService>();
        var projectId = Guid.NewGuid();
        var project = new ProjectDetailsDto(
            projectId,
            "Apollo",
            "Landing mission",
            "Active",
            Guid.NewGuid(),
            "Alice Smith",
            DateTimeOffset.UtcNow,
            null);
        readService.GetProjectByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);
        var handler = new GetProjectQueryHandler(readService);

        var result = await handler.Handle(new GetProjectQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(project);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundFailure_WhenProjectDoesNotExist()
    {
        var readService = Substitute.For<IProjectReadService>();
        var projectId = Guid.NewGuid();
        readService.GetProjectByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((ProjectDetailsDto?)null);
        var handler = new GetProjectQueryHandler(readService);

        var result = await handler.Handle(new GetProjectQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.NotFound);
    }
}
