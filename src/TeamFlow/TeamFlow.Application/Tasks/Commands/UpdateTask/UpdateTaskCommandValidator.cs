using FluentValidation;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.TaskId)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Description)
            .NotNull()
            .MaximumLength(2000);

        RuleFor(command => command.AssignedUserId)
            .Must(assignedUserId => assignedUserId is null || assignedUserId != Guid.Empty)
            .WithMessage("Assigned user ID must not be empty.");

        RuleFor(command => command.DueDate)
            .Must(dueDate => dueDate is null || dueDate > dateTimeProvider.UtcNow)
            .WithMessage("Due date must be in the future.");
    }
}
