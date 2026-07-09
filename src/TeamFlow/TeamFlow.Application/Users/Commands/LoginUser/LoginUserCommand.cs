using MediatR;

namespace TeamFlow.Application.Users.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<LoginUserResult>;

public record LoginUserResult(string Token, Guid UserId, string Role);
