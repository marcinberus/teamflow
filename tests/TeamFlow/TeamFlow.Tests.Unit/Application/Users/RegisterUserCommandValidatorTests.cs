using FluentAssertions;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Tests.Unit.Application.Users;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Developer")]
    public void Validate_ShouldSucceed_WhenRoleUsesExactEnumCasing(string role)
    {
        var result = _validator.Validate(ValidCommand(role));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("developer")]
    [InlineData("999")]
    public void Validate_ShouldFail_WhenRoleIsNotAnExactlyCasedDefinedValue(string role)
    {
        var result = _validator.Validate(ValidCommand(role));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Role");
    }

    private static RegisterUserCommand ValidCommand(string role) =>
        new("alice@example.com", "P@ssw0rd!", "Alice", "Smith", role);
}
