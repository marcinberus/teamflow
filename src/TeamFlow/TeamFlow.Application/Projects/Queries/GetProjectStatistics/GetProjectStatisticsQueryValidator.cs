using FluentValidation;

namespace TeamFlow.Application.Projects.Queries.GetProjectStatistics;

public sealed class GetProjectStatisticsQueryValidator : AbstractValidator<GetProjectStatisticsQuery>
{
    public GetProjectStatisticsQueryValidator()
    {
        RuleFor(query => query.ProjectId)
            .NotEmpty();
    }
}
