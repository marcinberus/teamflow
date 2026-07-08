using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Database;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TeamFlowDbContext _context;

    public UnitOfWork(TeamFlowDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
