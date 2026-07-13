using FluentAssertions;
using TeamFlow.Application.Projects.Queries.GetProject;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class GetProjectQueryValidatorTests
{
    private readonly GetProjectQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(new GetProjectQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetProjectQuery.ProjectId));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenProjectIdIsProvided()
    {
        var result = _validator.Validate(new GetProjectQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
