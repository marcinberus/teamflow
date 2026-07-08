using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Infrastructure.Database.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly TeamFlowDbContext _context;

    public UserRepository(TeamFlowDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Users.FindAsync([id], cancellationToken).AsTask();
}
