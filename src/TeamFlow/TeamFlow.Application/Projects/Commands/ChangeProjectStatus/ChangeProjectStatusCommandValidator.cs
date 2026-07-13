using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.ChangeProjectStatus;

public sealed class ChangeProjectStatusCommandValidator : AbstractValidator<ChangeProjectStatusCommand>
{
    public ChangeProjectStatusCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.Status)
            .NotEmpty()
            .Must(BeValidProjectStatus)
            .WithMessage("Status must be a valid project status.");
    }

    private static bool BeValidProjectStatus(string status)
    {
        return Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }
}
