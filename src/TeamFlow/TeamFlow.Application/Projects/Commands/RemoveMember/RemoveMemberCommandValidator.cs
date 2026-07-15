using FluentValidation;

namespace TeamFlow.Application.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
