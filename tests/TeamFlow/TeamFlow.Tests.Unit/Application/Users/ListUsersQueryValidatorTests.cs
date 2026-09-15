using FluentAssertions;
using TeamFlow.Application.Users.Queries.ListUsers;

namespace TeamFlow.Tests.Unit.Application.Users;

public sealed class ListUsersQueryValidatorTests
{
    private readonly ListUsersQueryValidator _validator = new();

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_ShouldFail_WhenPaginationIsInvalid(int page, int pageSize)
    {
        var result = _validator.Validate(new ListUsersQuery(Page: page, PageSize: pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSearchExceedsMaximumLength()
    {
        var result = _validator.Validate(new ListUsersQuery(new string('a', 257)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Search");
    }
}
