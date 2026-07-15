using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.AssignMember;

public sealed record AssignMemberCommand(
    Guid ProjectId,
    Guid UserId,
    string ProjectRole) : IRequest<Result<AssignMemberResult>>;

public sealed record AssignMemberResult(Guid MemberId);

public sealed record AssignMemberRequest(
    Guid UserId,
    string ProjectRole);
