using FluentAssertions;
using TeamFlow.Application.Projects.Commands.CreateProject;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        var result = _validator.Validate(new CreateProjectCommand(string.Empty, "Landing mission"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var result = _validator.Validate(new CreateProjectCommand("Apollo", new string('a', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }
}
