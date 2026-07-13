using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.DTOs;

namespace TeamFlow.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<Result<UserProfileDto>>;
