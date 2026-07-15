using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid ProjectId,
    Guid UserId) : IRequest<Result<RemoveMemberResult>>;

public sealed record RemoveMemberResult;
