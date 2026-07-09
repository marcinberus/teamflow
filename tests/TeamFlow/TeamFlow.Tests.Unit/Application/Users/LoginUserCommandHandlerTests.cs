using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.Commands.LoginUser;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Tests.Unit.Application.Users;

public class LoginUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _handler = new LoginUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _jwtTokenGenerator);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokenUserIdAndRole_WhenCredentialsAreValid()
    {
        var user = User.Create("alice@example.com", "hashed", "Alice", "Smith", Role.Developer, DateTimeOffset.UtcNow);
        var command = new LoginUserCommand("alice@example.com", "P@ssw0rd!");

        _userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify(command.Password, user.PasswordHash)
            .Returns(true);
        _jwtTokenGenerator
            .GenerateToken(user.Id, user.Email, "Developer")
            .Returns("token123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Token.Should().Be("token123");
        result.UserId.Should().Be(user.Id);
        result.Role.Should().Be("Developer");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenEmailNotFound()
    {
        var command = new LoginUserCommand("unknown@example.com", "P@ssw0rd!");

        _userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
    {
        var user = User.Create("alice@example.com", "hashed", "Alice", "Smith", Role.Developer, DateTimeOffset.UtcNow);
        var command = new LoginUserCommand("alice@example.com", "WrongPassword!");

        _userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify(command.Password, user.PasswordHash)
            .Returns(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_ShouldNotCallGenerateToken_WhenPasswordIsIncorrect()
    {
        var user = User.Create("alice@example.com", "hashed", "Alice", "Smith", Role.Developer, DateTimeOffset.UtcNow);
        var command = new LoginUserCommand("alice@example.com", "WrongPassword!");

        _userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify(command.Password, user.PasswordHash)
            .Returns(false);

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>();

        _jwtTokenGenerator
            .DidNotReceive()
            .GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_ShouldCallGenerateToken_WithCorrectParameters()
    {
        var user = User.Create("alice@example.com", "hashed", "Alice", "Smith", Role.Manager, DateTimeOffset.UtcNow);
        var command = new LoginUserCommand("alice@example.com", "P@ssw0rd!");

        _userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher
            .Verify(command.Password, user.PasswordHash)
            .Returns(true);
        _jwtTokenGenerator
            .GenerateToken(user.Id, user.Email, "Manager")
            .Returns("mgr-token");

        await _handler.Handle(command, CancellationToken.None);

        _jwtTokenGenerator
            .Received(1)
            .GenerateToken(user.Id, user.Email, "Manager");
    }
}
