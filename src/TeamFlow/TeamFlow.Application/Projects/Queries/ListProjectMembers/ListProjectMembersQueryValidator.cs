using FluentValidation;

namespace TeamFlow.Application.Projects.Queries.ListProjectMembers;

public sealed class ListProjectMembersQueryValidator : AbstractValidator<ListProjectMembersQuery>
{
    public ListProjectMembersQueryValidator()
    {
        RuleFor(query => query.ProjectId)
            .NotEmpty();
    }
}
