using MediatR;

namespace TeamFlow.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role) : IRequest<RegisterUserResult>;

public record RegisterUserResult(string Token, Guid UserId);
