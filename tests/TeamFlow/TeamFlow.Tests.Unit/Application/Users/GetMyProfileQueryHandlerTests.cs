using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Application.Users.Queries.GetMyProfile;

namespace TeamFlow.Tests.Unit.Application.Users;

public class GetMyProfileQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserReadService _userReadService = Substitute.For<IUserReadService>();

    private readonly GetMyProfileQueryHandler _handler;

    public GetMyProfileQueryHandlerTests()
    {
        _handler = new GetMyProfileQueryHandler(_currentUserService, _userReadService);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserProfile_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfileDto(userId, "alice@example.com", "Alice", "Smith", "Developer", DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(userId);
        _userReadService.GetProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await _handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(profile);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();

        _currentUserService.UserId.Returns(userId);
        _userReadService.GetProfileAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfileDto?)null);

        var result = await _handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
    }
}
