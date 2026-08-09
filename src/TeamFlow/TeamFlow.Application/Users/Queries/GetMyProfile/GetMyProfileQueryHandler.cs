using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Application.Users.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(
    ICurrentUserService currentUserService,
    IUserReadService userReadService,
    ILogger<GetMyProfileQueryHandler> logger) : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var profile = await userReadService.GetProfileAsync(userId, cancellationToken);

        if (profile is null)
        {
            logger.LogInformation("Profile for user {UserId} was not found.", userId);
            return Result<UserProfileDto>.Failure(ErrorMessages.NotFound);
        }

        logger.LogInformation("Profile for user {UserId} was retrieved.", userId);
        return Result<UserProfileDto>.Success(profile);
    }
}
