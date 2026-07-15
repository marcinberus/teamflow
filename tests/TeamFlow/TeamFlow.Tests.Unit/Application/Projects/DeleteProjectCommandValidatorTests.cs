using FluentAssertions;
using TeamFlow.Application.Projects.Commands.DeleteProject;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenProjectIdIsNotEmpty()
    {
        var result = _validator.Validate(new DeleteProjectCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(new DeleteProjectCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }
}
