using FluentValidation;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(command => command.Description)
            .MaximumLength(2000);

        RuleFor(command => command.DueDate)
            .Must(dueDate => dueDate is null || dueDate > dateTimeProvider.UtcNow)
            .WithMessage("Due date must be in the future.");
    }
}
