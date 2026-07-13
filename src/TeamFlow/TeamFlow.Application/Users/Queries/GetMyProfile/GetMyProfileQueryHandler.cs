using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Application.Users.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(
    ICurrentUserService currentUserService,
    IUserReadService userReadService) : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var profile = await userReadService.GetProfileAsync(userId, cancellationToken);

        return profile is null
            ? Result<UserProfileDto>.Failure(ErrorMessages.NotFound)
            : Result<UserProfileDto>.Success(profile);
    }
}
