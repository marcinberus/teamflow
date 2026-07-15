using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.ListProjectMembers;

public sealed class ListProjectMembersQueryHandler(IProjectReadService projectReadService)
    : IRequestHandler<ListProjectMembersQuery, Result<ListProjectMembersResult>>
{
    public async Task<Result<ListProjectMembersResult>> Handle(
        ListProjectMembersQuery request,
        CancellationToken cancellationToken)
    {
        var members = await projectReadService.ListMembersAsync(
            request.ProjectId,
            cancellationToken);

        return Result<ListProjectMembersResult>.Success(
            new ListProjectMembersResult(members));
    }
}
