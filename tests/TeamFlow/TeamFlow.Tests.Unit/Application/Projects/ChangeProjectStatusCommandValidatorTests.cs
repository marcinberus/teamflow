using FluentAssertions;
using TeamFlow.Application.Projects.Commands.ChangeProjectStatus;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class ChangeProjectStatusCommandValidatorTests
{
    private readonly ChangeProjectStatusCommandValidator _validator = new();

    [Theory]
    [InlineData("Active")]
    [InlineData("OnHold")]
    [InlineData("completed")]
    public void Validate_ShouldSucceed_WhenStatusIsValid(string status)
    {
        var result = _validator.Validate(
            new ChangeProjectStatusCommand(Guid.NewGuid(), status));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    [InlineData("999")]
    public void Validate_ShouldFail_WhenStatusIsInvalid(string status)
    {
        var result = _validator.Validate(
            new ChangeProjectStatusCommand(Guid.NewGuid(), status));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(
            new ChangeProjectStatusCommand(Guid.Empty, "OnHold"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }
}
