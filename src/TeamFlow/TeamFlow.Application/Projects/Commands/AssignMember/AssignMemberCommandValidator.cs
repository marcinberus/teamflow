using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.AssignMember;

public sealed class AssignMemberCommandValidator : AbstractValidator<AssignMemberCommand>
{
    private static readonly string[] ValidRoles = Enum.GetNames<Role>();

    public AssignMemberCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.ProjectRole)
            .NotEmpty()
            .Must(role => ValidRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}.");
    }
}
