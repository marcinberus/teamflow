using FluentAssertions;
using TeamFlow.Application.Projects.Queries.ListProjectMembers;

namespace TeamFlow.Tests.Unit.Application.Projects.Queries;

public sealed class ListProjectMembersQueryValidatorTests
{
    private readonly ListProjectMembersQueryValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceed_WhenProjectIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ListProjectMembersQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(
            new ListProjectMembersQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }
}
