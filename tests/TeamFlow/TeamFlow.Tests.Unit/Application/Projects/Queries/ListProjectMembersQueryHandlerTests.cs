using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Projects.Queries.ListProjectMembers;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class ListProjectMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMembersFromReadService()
    {
        var projectId = Guid.NewGuid();
        var members = new List<ProjectMemberDto>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "alice@example.com",
                "Alice",
                "Smith",
                "Manager",
                "Developer",
                DateTimeOffset.UtcNow)
        };
        var readService = Substitute.For<IProjectReadService>();
        readService.ListMembersAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(members);
        var handler = new ListProjectMembersQueryHandler(readService);

        var result = await handler.Handle(
            new ListProjectMembersQuery(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEquivalentTo(members);
        result.Error.Should().BeNull();
        await readService.Received(1).ListMembersAsync(
            projectId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyItems_WhenProjectHasNoMembers()
    {
        var projectId = Guid.NewGuid();
        var readService = Substitute.For<IProjectReadService>();
        readService.ListMembersAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProjectMemberDto>());
        var handler = new ListProjectMembersQueryHandler(readService);

        var result = await handler.Handle(
            new ListProjectMembersQuery(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
