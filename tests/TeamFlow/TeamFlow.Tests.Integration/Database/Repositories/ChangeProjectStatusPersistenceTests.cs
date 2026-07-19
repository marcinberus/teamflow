using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class ChangeProjectStatusPersistenceTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private ProjectRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public ChangeProjectStatusPersistenceTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _fixture = fixture.Database;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var options = new DbContextOptionsBuilder<TeamFlowDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _db = new TeamFlowDbContext(options);
        _transaction = await _db.Database.BeginTransactionAsync();
        _repository = new ProjectRepository(_db);
        _unitOfWork = new UnitOfWork(_db);
    }

    public override async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task ChangeStatus_ShouldPersistStatusAndTimestamp_WhenUnitOfWorkSavesChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var updatedAt = now.AddMinutes(1);
        var owner = User.Create(
            "change-project-status-persistence@example.com",
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
        loadedProject!.ChangeStatus(ProjectStatus.OnHold, updatedAt);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedProject = await _db.Projects
            .AsNoTracking()
            .SingleAsync(item => item.Id == project.Id);
        persistedProject.Status.Should().Be(ProjectStatus.OnHold);
        persistedProject.UpdatedAt.Should().Be(updatedAt);
    }
}
