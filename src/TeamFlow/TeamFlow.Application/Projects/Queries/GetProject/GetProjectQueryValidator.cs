using FluentValidation;

namespace TeamFlow.Application.Projects.Queries.GetProject;

public sealed class GetProjectQueryValidator : AbstractValidator<GetProjectQuery>
{
    public GetProjectQueryValidator()
    {
        RuleFor(query => query.ProjectId)
            .NotEmpty();
    }
}
