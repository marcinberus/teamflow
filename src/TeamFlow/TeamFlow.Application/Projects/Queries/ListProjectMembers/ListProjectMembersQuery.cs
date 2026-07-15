using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Queries.ListProjectMembers;

public sealed record ListProjectMembersQuery(Guid ProjectId)
    : IRequest<Result<ListProjectMembersResult>>;

public sealed record ListProjectMembersResult(IReadOnlyList<ProjectMemberDto> Items);
