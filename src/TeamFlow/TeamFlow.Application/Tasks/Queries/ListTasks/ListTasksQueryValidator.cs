using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tasks.Queries.ListTasks;

public sealed class ListTasksQueryValidator : AbstractValidator<ListTasksQuery>
{
    public ListTasksQueryValidator()
    {
        RuleFor(query => query.ProjectId)
            .NotEmpty();

        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status)
                || Enum.TryParse<TaskItemStatus>(status, true, out _))
            .WithMessage("Status must be a valid task status.");

        RuleFor(query => query.AssignedUserId)
            .Must(assignedUserId => assignedUserId is null || assignedUserId != Guid.Empty)
            .WithMessage("Assigned user ID must not be empty.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
