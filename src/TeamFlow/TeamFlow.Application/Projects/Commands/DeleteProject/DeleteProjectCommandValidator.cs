using FluentValidation;

namespace TeamFlow.Application.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();
    }
}
