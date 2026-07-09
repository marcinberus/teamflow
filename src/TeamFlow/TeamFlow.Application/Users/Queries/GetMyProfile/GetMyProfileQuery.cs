using MediatR;
using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<UserProfileDto>;
