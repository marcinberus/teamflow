using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role) : IRequest<Result<RegisterUserResult>>;

public record RegisterUserResult(string Token, Guid UserId);
