using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
