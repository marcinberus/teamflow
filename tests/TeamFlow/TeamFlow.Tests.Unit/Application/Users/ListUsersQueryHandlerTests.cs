using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Application.Users.Queries.ListUsers;

namespace TeamFlow.Tests.Unit.Application.Users;

public sealed class ListUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPaginatedUsers_FromReadService()
    {
        var readService = Substitute.For<IUserReadService>();
        IReadOnlyList<UserSummaryDto> users =
        [
            new(Guid.NewGuid(), "alice@example.com", "Alice", "Smith", "Developer")
        ];
        readService.ListUsersAsync("alice", 2, 10, Arg.Any<CancellationToken>())
            .Returns((users, 15));

        var handler = new ListUsersQueryHandler(readService);

        var result = await handler.Handle(new ListUsersQuery("alice", 2, 10), CancellationToken.None);

        result.Items.Should().Equal(users);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }
}
