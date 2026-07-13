using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Queries.ListProjects;

public sealed class ListProjectsQueryValidator : AbstractValidator<ListProjectsQuery>
{
    public ListProjectsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || Enum.TryParse<ProjectStatus>(status, true, out _))
            .WithMessage("Status must be a valid project status.");
    }
}
