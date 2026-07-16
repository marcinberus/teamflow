using FluentAssertions;
using TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class ChangeTaskStatusCommandValidatorTests
{
    private readonly ChangeTaskStatusCommandValidator _validator = new();

    [Theory]
    [InlineData("Todo")]
    [InlineData("InProgress")]
    [InlineData("Verification")]
    [InlineData("Done")]
    [InlineData("Cancelled")]
    public void Validate_ShouldSucceed_WhenStatusIsValid(string status)
    {
        var result = _validator.Validate(
            new ChangeTaskStatusCommand(Guid.NewGuid(), Guid.NewGuid(), status));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("verification")]
    [InlineData("Unknown")]
    [InlineData("999")]
    public void Validate_ShouldFail_WhenStatusIsInvalid(string status)
    {
        var result = _validator.Validate(
            new ChangeTaskStatusCommand(Guid.NewGuid(), Guid.NewGuid(), status));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(
            new ChangeTaskStatusCommand(Guid.Empty, Guid.NewGuid(), "Done"));

        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTaskIdIsEmpty()
    {
        var result = _validator.Validate(
            new ChangeTaskStatusCommand(Guid.NewGuid(), Guid.Empty, "Done"));

        result.Errors.Should().Contain(error => error.PropertyName == "TaskId");
    }
}
