using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Users.Interfaces;

namespace TeamFlow.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResult>>
{
    public async Task<Result<UpdateProfileResult>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UpdateProfileResult>.Failure(ErrorMessages.NotFound);
        }

        user.UpdateProfile(request.FirstName, request.LastName, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateProfileResult>.Success(new());
    }
}
