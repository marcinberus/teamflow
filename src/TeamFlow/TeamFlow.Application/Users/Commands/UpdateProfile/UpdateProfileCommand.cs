using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(string FirstName, string LastName) : IRequest<Result<UpdateProfileResult>>;

public record UpdateProfileResult();
