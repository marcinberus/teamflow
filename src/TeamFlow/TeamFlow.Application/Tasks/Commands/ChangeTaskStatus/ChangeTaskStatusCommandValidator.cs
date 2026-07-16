using FluentValidation;
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
            .Must(BeValidTaskStatus)
            .WithMessage("Status must be a valid task status.");
    }

    private static bool BeValidTaskStatus(string status)
    {
        return Enum.TryParse<TaskItemStatus>(status, ignoreCase: true, out var parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }
}
