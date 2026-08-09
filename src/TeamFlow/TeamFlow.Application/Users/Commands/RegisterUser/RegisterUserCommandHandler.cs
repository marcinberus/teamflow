using MediatR;
using Microsoft.Extensions.Logging;
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
    IDateTimeProvider dateTimeProvider,
    ILogger<RegisterUserCommandHandler> logger) : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            logger.LogInformation("Registration was rejected because email {Email} is already in use.", request.Email);
            return Result<RegisterUserResult>.Failure(ErrorMessages.EmailAlreadyExists);
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        if (!Enum.TryParse<Role>(request.Role, out var role))
        {
            logger.LogWarning("Registration for email {Email} was rejected because role {Role} is invalid.", request.Email, request.Role);
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

        logger.LogInformation("User {UserId} registered with role {Role}.", user.Id, user.Role);
        return Result<RegisterUserResult>.Success(new RegisterUserResult(token, user.Id));
    }
}
