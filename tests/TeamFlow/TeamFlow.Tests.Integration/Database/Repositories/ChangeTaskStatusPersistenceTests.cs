using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class ChangeTaskStatusPersistenceTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;
    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private TaskItemRepository _taskItemRepository = null!;
    private UnitOfWork _unitOfWork = null!;

    public ChangeTaskStatusPersistenceTests(IntegrationTestFixture fixture) : base(fixture)
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
        _taskItemRepository = new TaskItemRepository(_db);
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
            $"change-task-status-{Guid.NewGuid():N}@example.com",
            "hash",
            "Test",
            "Owner",
            Role.Manager,
            now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);
        var task = TaskItem.Create(project.Id, "Design API", string.Empty, owner.Id, null, now);

        _db.Users.Add(owner);
        _db.Projects.Add(project);
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var loadedTask = await _taskItemRepository.GetByIdAsync(task.Id, CancellationToken.None);
        loadedTask!.ChangeStatus(TaskItemStatus.Verification, updatedAt);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedTask = await _db.Tasks
            .AsNoTracking()
            .SingleAsync(item => item.Id == task.Id);

        persistedTask.Status.Should().Be(TaskItemStatus.Verification);
        persistedTask.UpdatedAt.Should().Be(updatedAt);
    }
}
