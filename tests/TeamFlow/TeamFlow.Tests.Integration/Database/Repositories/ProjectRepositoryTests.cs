using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProjectRepositoryTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private ProjectRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public ProjectRepositoryTests(IntegrationTestFixture fixture) : base(fixture)
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
    public async Task GetByIdAsync_ShouldReturnTrackedProject_WhenProjectExists()
    {
        var project = await SeedProjectAsync("project-repository-get@example.com");

        var result = await _repository.GetByIdAsync(project.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
        result.Name.Should().Be("Apollo");
        result.Description.Should().Be("Landing mission");
        result.OwnerId.Should().Be(project.OwnerId);
        _db.Entry(result).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPersistDomainUpdate_WhenUnitOfWorkSavesChanges()
    {
        var project = await SeedProjectAsync("project-repository-update@example.com");
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(1);
        var loadedProject = await _repository.GetByIdAsync(project.Id, CancellationToken.None);

        loadedProject!.Update("Apollo v2", "Updated mission", updatedAt);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedProject = await _db.Projects
            .AsNoTracking()
            .SingleAsync(item => item.Id == project.Id);
        persistedProject.Name.Should().Be("Apollo v2");
        persistedProject.Description.Should().Be("Updated mission");
        persistedProject.UpdatedAt.Should().Be(updatedAt);
    }

    private async Task<Project> SeedProjectAsync(string ownerEmail)
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create(ownerEmail, "hash", "Test", "Owner", Role.Manager, now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);

        _db.Users.Add(owner);
        await _repository.AddAsync(project, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        return project;
    }
}
