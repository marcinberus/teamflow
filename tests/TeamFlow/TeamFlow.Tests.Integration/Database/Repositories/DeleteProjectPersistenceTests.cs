using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

public sealed class DeleteProjectPersistenceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private ProjectRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public DeleteProjectPersistenceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TeamFlowDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _db = new TeamFlowDbContext(options);
        _transaction = await _db.Database.BeginTransactionAsync();
        _repository = new ProjectRepository(_db);
        _unitOfWork = new UnitOfWork(_db);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPermanentlyRemoveProject_WhenUnitOfWorkSavesChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create(
            "delete-project-persistence@example.com",
            "hash",
            "Test",
            "Owner",
            Role.Manager,
            now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);

        _db.Users.Add(owner);
        await _repository.AddAsync(project, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loadedProject = await _repository.GetByIdAsync(project.Id, CancellationToken.None);
        await _repository.DeleteAsync(loadedProject!, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var projectExists = await _db.Projects
            .AsNoTracking()
            .AnyAsync(item => item.Id == project.Id);
        projectExists.Should().BeFalse();
    }
}
