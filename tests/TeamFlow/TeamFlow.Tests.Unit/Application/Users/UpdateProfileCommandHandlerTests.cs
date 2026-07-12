using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.Commands.UpdateProfile;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Users;

public class UpdateProfileCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _currentUserService,
            _userRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProfile_WhenInputIsValid()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand("Alicia", "Jones");
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("alice@example.com", "hash", "Alice", "Smith", Role.Developer, now);

        _currentUserService.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("Alicia");
        user.LastName.Should().Be("Jones");
        user.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_ShouldSaveChanges_WhenInputIsValid()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand("Alicia", "Jones");
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("alice@example.com", "hash", "Alice", "Smith", Role.Developer, now);

        _currentUserService.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateProfileCommand("Alicia", "Jones");

        _currentUserService.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(null as User);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
    }
}
