using FluentValidation;
using TeamFlow.Application.Common.Validation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.TaskId)
            .NotEmpty();

        RuleFor(command => command.Status)
            .NotEmpty()
            .Must(EnumValidation.IsDefinedValue<TaskItemStatus>)
            .WithMessage("Status must be a valid task status.");
    }
}
