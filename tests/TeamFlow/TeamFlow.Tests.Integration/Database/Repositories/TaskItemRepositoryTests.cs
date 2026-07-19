using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class TaskItemRepositoryTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;
    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private TaskItemRepository _taskItemRepository = null!;
    private UnitOfWork _unitOfWork = null!;

    public TaskItemRepositoryTests(IntegrationTestFixture fixture) : base(fixture)
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
    public async Task AddAsync_ShouldPersistTaskWithAssignmentAndDueDate()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser($"task-owner-{Guid.NewGuid():N}@example.com", now);
        var assignee = CreateUser($"task-assignee-{Guid.NewGuid():N}@example.com", now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);
        project.AssignMember(assignee.Id, Role.Developer, now);
        _db.Users.AddRange(owner, assignee);
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var dueDate = now.AddDays(7);
        var task = TaskItem.Create(
            project.Id,
            "Design API",
            "Define endpoints",
            assignee.Id,
            dueDate,
            now);
        await _taskItemRepository.AddAsync(task, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedTask = await _db.Tasks
            .AsNoTracking()
            .SingleAsync(item => item.Id == task.Id);

        persistedTask.ProjectId.Should().Be(project.Id);
        persistedTask.Title.Should().Be("Design API");
        persistedTask.Description.Should().Be("Define endpoints");
        persistedTask.AssignedUserId.Should().Be(assignee.Id);
        persistedTask.DueDate.Should().Be(dueDate);
        persistedTask.Status.Should().Be(TaskItemStatus.Todo);
        persistedTask.CreatedAt.Should().Be(now);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadTaskAndPersistUpdates()
    {
        var now = DateTimeOffset.UtcNow;
        var updatedAt = now.AddHours(1);
        var owner = CreateUser($"task-update-owner-{Guid.NewGuid():N}@example.com", now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);
        var task = TaskItem.Create(
            project.Id,
            "Design API",
            "Define endpoints",
            owner.Id,
            now.AddDays(1),
            now);
        _db.Users.Add(owner);
        _db.Projects.Add(project);
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var loadedTask = await _taskItemRepository.GetByIdAsync(task.Id, CancellationToken.None);
        loadedTask.Should().NotBeNull();
        loadedTask!.Update("Design REST API", "Updated endpoints", null, null, updatedAt);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedTask = await _db.Tasks
            .AsNoTracking()
            .SingleAsync(item => item.Id == task.Id);

        persistedTask.Title.Should().Be("Design REST API");
        persistedTask.Description.Should().Be("Updated endpoints");
        persistedTask.AssignedUserId.Should().BeNull();
        persistedTask.DueDate.Should().BeNull();
        persistedTask.UpdatedAt.Should().Be(updatedAt);
    }

    private static User CreateUser(string email, DateTimeOffset now) =>
        User.Create(email, "hash", "Test", "User", Role.Developer, now);
}
