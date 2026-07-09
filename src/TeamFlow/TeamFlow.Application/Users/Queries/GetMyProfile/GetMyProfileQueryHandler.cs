using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.DTOs;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Application.Users.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(
    ICurrentUserService currentUserService,
    IUserReadService userReadService) : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var profile = await userReadService.GetProfileAsync(userId, cancellationToken);

        return profile is null 
            ? throw new NotFoundException($"User with id '{userId}' was not found.") 
            : profile;
    }
}
