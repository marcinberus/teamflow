using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Tasks.Commands.UpdateTask;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class UpdateTaskCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly UpdateTaskCommandValidator _validator;

    public UpdateTaskCommandValidatorTests()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);
        _validator = new UpdateTaskCommandValidator(dateTimeProvider);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenInputIsValid()
    {
        var result = _validator.Validate(ValidCommand(dueDate: Now.AddDays(1)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenProjectIdIsEmpty()
    {
        var result = _validator.Validate(ValidCommand(projectId: Guid.Empty));

        result.Errors.Should().Contain(error => error.PropertyName == "ProjectId");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTaskIdIsEmpty()
    {
        var result = _validator.Validate(ValidCommand(taskId: Guid.Empty));

        result.Errors.Should().Contain(error => error.PropertyName == "TaskId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenTitleIsEmpty(string title)
    {
        var result = _validator.Validate(ValidCommand(title: title));

        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceedsMaximumLength()
    {
        var result = _validator.Validate(ValidCommand(title: new string('a', 301)));

        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionIsNull()
    {
        var result = _validator.Validate(ValidCommand(description: null!));

        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceedsMaximumLength()
    {
        var result = _validator.Validate(ValidCommand(description: new string('a', 2001)));

        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAssignedUserIdIsEmpty()
    {
        var result = _validator.Validate(ValidCommand(assignedUserId: Guid.Empty));

        result.Errors.Should().Contain(error => error.PropertyName == "AssignedUserId");
    }

    [Theory]
    [MemberData(nameof(InvalidDueDates))]
    public void Validate_ShouldFail_WhenDueDateIsNotInFuture(DateTimeOffset dueDate)
    {
        var result = _validator.Validate(ValidCommand(dueDate: dueDate));

        result.Errors.Should().Contain(error => error.PropertyName == "DueDate");
    }

    public static TheoryData<DateTimeOffset> InvalidDueDates => new()
    {
        Now,
        Now.AddTicks(-1)
    };

    private static UpdateTaskCommand ValidCommand(
        Guid? projectId = null,
        Guid? taskId = null,
        string title = "Design API",
        string description = "Define endpoints",
        Guid? assignedUserId = null,
        DateTimeOffset? dueDate = null) =>
        new(
            projectId ?? Guid.NewGuid(),
            taskId ?? Guid.NewGuid(),
            title,
            description,
            assignedUserId,
            dueDate);
}
