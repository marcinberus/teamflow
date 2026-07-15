using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Tasks.Commands.CreateTask;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class CreateTaskCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateTaskCommandValidator _validator;

    public CreateTaskCommandValidatorTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _validator = new CreateTaskCommandValidator(_dateTimeProvider);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenInputIsValid()
    {
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            "Design API",
            "Define endpoints",
            Guid.NewGuid(),
            Now.AddDays(1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenTitleIsEmpty(string title)
    {
        var result = _validator.Validate(
            new CreateTaskCommand(Guid.NewGuid(), title, string.Empty, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceedsMaximumLength()
    {
        var result = _validator.Validate(
            new CreateTaskCommand(Guid.NewGuid(), new string('a', 301), string.Empty, null, null));

        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var result = _validator.Validate(
            new CreateTaskCommand(Guid.NewGuid(), "Design API", new string('a', 2001), null, null));

        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    [Theory]
    [MemberData(nameof(InvalidDueDates))]
    public void Validate_ShouldFail_WhenDueDateIsNotInFuture(DateTimeOffset dueDate)
    {
        var result = _validator.Validate(
            new CreateTaskCommand(Guid.NewGuid(), "Design API", string.Empty, null, dueDate));

        result.Errors.Should().Contain(error => error.PropertyName == "DueDate");
    }

    public static TheoryData<DateTimeOffset> InvalidDueDates => new()
    {
        Now,
        Now.AddTicks(-1)
    };
}
