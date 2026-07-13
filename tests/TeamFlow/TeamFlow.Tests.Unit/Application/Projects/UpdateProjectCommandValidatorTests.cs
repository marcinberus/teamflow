using FluentAssertions;
using TeamFlow.Application.Projects.Commands.UpdateProject;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(
            new UpdateProjectCommand(Guid.Empty, "Apollo", "Landing mission"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = _validator.Validate(
            new UpdateProjectCommand(Guid.NewGuid(), string.Empty, "Landing mission"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var result = _validator.Validate(
            new UpdateProjectCommand(Guid.NewGuid(), "Apollo", new string('a', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }
}
