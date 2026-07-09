using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new LoginUserResult(token, user.Id, user.Role.ToString());
    }
}
