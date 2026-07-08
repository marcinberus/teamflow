using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email address already exists.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        if (!Enum.TryParse<Role>(request.Role, out var role))
        {
            throw new ArgumentException($"Invalid role: {request.Role}.");
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

        return new RegisterUserResult(token, user.Id);
    }
}
