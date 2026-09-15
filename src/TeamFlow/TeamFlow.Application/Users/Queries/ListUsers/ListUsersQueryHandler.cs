using MediatR;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Application.Users.Queries.ListUsers;

public sealed class ListUsersQueryHandler(IUserReadService userReadService)
    : IRequestHandler<ListUsersQuery, ListUsersResult>
{
    public async Task<ListUsersResult> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await userReadService.ListUsersAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new ListUsersResult(items, totalCount, request.Page, request.PageSize);
    }
}
