using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.ListProjectMembers;

public sealed class ListProjectMembersQueryHandler(
    IProjectReadService projectReadService,
    ILogger<ListProjectMembersQueryHandler> logger)
    : IRequestHandler<ListProjectMembersQuery, Result<ListProjectMembersResult>>
{
    public async Task<Result<ListProjectMembersResult>> Handle(
        ListProjectMembersQuery request,
        CancellationToken cancellationToken)
    {
        var members = await projectReadService.ListMembersAsync(
            request.ProjectId,
            cancellationToken);

        logger.LogInformation(
            "Retrieved {MemberCount} members for project {ProjectId}.",
            members.Count,
            request.ProjectId);
        return Result<ListProjectMembersResult>.Success(
            new ListProjectMembersResult(members));
    }
}
