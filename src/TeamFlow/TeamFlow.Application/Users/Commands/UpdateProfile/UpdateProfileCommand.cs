using MediatR;

namespace TeamFlow.Application.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(string FirstName, string LastName) : IRequest;
