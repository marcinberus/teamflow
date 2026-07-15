using FluentAssertions;
using TeamFlow.Application.Tasks.Queries.ListTasks;

namespace TeamFlow.Tests.Unit.Application.Tasks.Queries;

public sealed class ListTasksQueryValidatorTests
{
    private readonly ListTasksQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }

    [Theory]
    [InlineData("Archived")]
    [InlineData("Waiting")]
    public void Validate_ShouldFail_WhenStatusIsInvalid(string status)
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.NewGuid(), status));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Status");
    }

    [Fact]
    public void Validate_ShouldAcceptStatus_IgnoringCase()
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.NewGuid(), "inprogress"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPageIsLessThanOne(int page)
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.NewGuid(), Page: page));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenPageSizeIsOutsideAllowedRange(int pageSize)
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.NewGuid(), PageSize: pageSize));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAssignedUserIdIsEmpty()
    {
        var result = _validator.Validate(new ListTasksQuery(Guid.NewGuid(), AssignedUserId: Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "AssignedUserId");
    }
}
