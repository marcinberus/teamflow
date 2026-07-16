using FluentValidation;
using TeamFlow.Application.Common.Validation;
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
            .Must(EnumValidation.IsDefinedValue<ProjectStatus>)
            .WithMessage("Status must be a valid project status.");
    }
}
