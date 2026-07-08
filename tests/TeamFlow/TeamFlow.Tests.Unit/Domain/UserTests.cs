using FluentAssertions;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Domain;

public sealed class UserTests
{
    [Fact]
    public void User_Create_ShouldSetAllProperties()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create("john@example.com", "passwordhash", "John", "Doe", Role.Developer, now);

        user.Email.Should().Be("john@example.com");
        user.PasswordHash.Should().Be("passwordhash");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Role.Should().Be(Role.Developer);
        user.CreatedAt.Should().Be(now);
        user.UpdatedAt.Should().BeNull();
        user.Id.Should().NotBeEmpty();
    }
}
