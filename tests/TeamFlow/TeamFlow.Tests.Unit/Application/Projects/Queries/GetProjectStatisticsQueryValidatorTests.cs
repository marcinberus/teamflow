using FluentAssertions;
using TeamFlow.Application.Projects.Queries.GetProjectStatistics;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class GetProjectStatisticsQueryValidatorTests
{
    private readonly GetProjectStatisticsQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(new GetProjectStatisticsQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(
            error => error.PropertyName == nameof(GetProjectStatisticsQuery.ProjectId));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenProjectIdIsProvided()
    {
        var result = _validator.Validate(new GetProjectStatisticsQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
