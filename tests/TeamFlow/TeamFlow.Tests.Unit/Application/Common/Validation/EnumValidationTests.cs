using FluentAssertions;
using TeamFlow.Application.Common.Validation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Common.Validation;

public sealed class EnumValidationTests
{
    [Theory]
    [InlineData("Todo")]
    [InlineData("InProgress")]
    [InlineData("Verification")]
    [InlineData("Done")]
    [InlineData("Cancelled")]
    public void IsDefinedValue_ShouldReturnTrue_WhenValueIsDefinedWithExactCasing(string value)
    {
        EnumValidation.IsDefinedValue<TaskItemStatus>(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("done")]
    [InlineData("1")]
    [InlineData("Unknown")]
    [InlineData("999")]
    public void IsDefinedValue_ShouldReturnFalse_WhenValueIsNotAnExactlyCasedDefinedValue(string value)
    {
        EnumValidation.IsDefinedValue<TaskItemStatus>(value).Should().BeFalse();
    }
}
