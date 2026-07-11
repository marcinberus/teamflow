using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result<RegisterUserResult>.Failure(ErrorMessages.EmailAlreadyExists);
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        if (!Enum.TryParse<Role>(request.Role, out var role))
        {
            return Result<RegisterUserResult>.Failure(ErrorMessages.InvalidRole);
        }

        var user = User.Create(
            request.Email,
            passwordHash,
            request.FirstName,
            request.LastName,
            role,
            dateTimeProvider.UtcNow);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return Result<RegisterUserResult>.Success(new RegisterUserResult(token, user.Id));
    }
}
