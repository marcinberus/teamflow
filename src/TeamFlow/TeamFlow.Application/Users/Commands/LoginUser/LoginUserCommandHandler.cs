using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginUserCommand, Result<LoginUserResult>>
{
    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginUserResult>.Failure(ErrorMessages.InvalidCredentials);
        }

        var token = jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return Result<LoginUserResult>.Success(new LoginUserResult(token, user.Id, user.Role.ToString()));
    }
}
