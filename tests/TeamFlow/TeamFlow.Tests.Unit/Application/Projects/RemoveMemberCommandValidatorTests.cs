using FluentAssertions;
using TeamFlow.Application.Projects.Commands.RemoveMember;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class RemoveMemberCommandValidatorTests
{
    private readonly RemoveMemberCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenIdsAreNotEmpty()
    {
        var result = _validator.Validate(
            new RemoveMemberCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(
            new RemoveMemberCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var result = _validator.Validate(
            new RemoveMemberCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
    }
}
