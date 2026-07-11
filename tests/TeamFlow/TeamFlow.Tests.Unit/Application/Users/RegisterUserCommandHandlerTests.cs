using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.Commands.RegisterUser;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Users;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(
            _userRepository,
            _unitOfWork,
            _passwordHasher,
            _jwtTokenGenerator,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldReturnTokenAndUserId_WhenInputIsValid()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");
        var now = DateTimeOffset.UtcNow;

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed");
        _dateTimeProvider.UtcNow.Returns(now);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<Guid>(), command.Email, "Developer")
            .Returns("token123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("token123");
        result.Value.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldAddUserToRepository_WhenInputIsValid()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed");
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("token");

        await _handler.Handle(command, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == command.Email &&
            u.FirstName == command.FirstName &&
            u.LastName == command.LastName &&
            u.Role == Role.Developer), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSaveChanges_WhenInputIsValid()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed");
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("token");

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.EmailAlreadyExists);
    }

    [Fact]
    public async Task Handle_ShouldNotSaveChanges_WhenEmailAlreadyExists()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(command, CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRoleIsInvalid()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "SuperAdmin");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.InvalidRole);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldHashPassword()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Developer");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed_password");
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("token");

        await _handler.Handle(command, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash == "hashed_password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateTokenWithCorrectClaims()
    {
        var command = new RegisterUserCommand("alice@example.com", "P@ssw0rd!", "Alice", "Smith", "Manager");

        _userRepository
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher
            .Hash(command.Password)
            .Returns("hashed");
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _jwtTokenGenerator
            .GenerateToken(Arg.Any<Guid>(), command.Email, "Manager")
            .Returns("manager_token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("manager_token");
        _jwtTokenGenerator.Received(1).GenerateToken(Arg.Any<Guid>(), command.Email, "Manager");
    }
}
