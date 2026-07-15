using FluentAssertions;
using TeamFlow.Application.Projects.Commands.AssignMember;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class AssignMemberCommandValidatorTests
{
    private readonly AssignMemberCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenInputIsValid()
    {
        var result = _validator.Validate(
            new AssignMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "Developer"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var result = _validator.Validate(
            new AssignMemberCommand(Guid.NewGuid(), Guid.Empty, "Developer"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("Contributor")]
    public void Validate_ShouldFail_WhenRoleIsInvalid(string role)
    {
        var result = _validator.Validate(
            new AssignMemberCommand(Guid.NewGuid(), Guid.NewGuid(), role));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectRole");
    }
}
