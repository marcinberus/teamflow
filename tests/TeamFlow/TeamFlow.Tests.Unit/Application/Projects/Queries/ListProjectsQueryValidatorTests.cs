using FluentAssertions;
using TeamFlow.Application.Projects.Queries.ListProjects;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class ListProjectsQueryValidatorTests
{
    private readonly ListProjectsQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageIsLessThanOne(int page)
    {
        var result = _validator.Validate(new ListProjectsQuery(page, 20));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenPageSizeIsOutsideAllowedRange(int pageSize)
    {
        var result = _validator.Validate(new ListProjectsQuery(1, pageSize));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_ShouldFail_WhenStatusIsInvalid()
    {
        var result = _validator.Validate(new ListProjectsQuery(1, 20, "Archived"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldAcceptStatus_IgnoringCase()
    {
        var result = _validator.Validate(new ListProjectsQuery(1, 20, "active"));

        result.IsValid.Should().BeTrue();
    }
}
