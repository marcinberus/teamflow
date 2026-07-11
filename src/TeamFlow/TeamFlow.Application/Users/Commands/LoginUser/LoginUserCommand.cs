using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Users.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<Result<LoginUserResult>>;

public record LoginUserResult(string Token, Guid UserId, string Role);
