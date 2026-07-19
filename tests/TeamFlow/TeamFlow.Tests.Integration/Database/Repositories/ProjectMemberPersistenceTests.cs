using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProjectMemberPersistenceTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private ProjectRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public ProjectMemberPersistenceTests(IntegrationTestFixture fixture) : base(fixture)
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
    public async Task GetByIdWithMembersAsync_ShouldLoadMembersAndPersistAssignedMember()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("assign-persistence-owner@example.com", Role.Manager, now);
        var existingMember = CreateUser("assign-persistence-existing@example.com", Role.Developer, now);
        var newMember = CreateUser("assign-persistence-new@example.com", Role.Developer, now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);
        project.AssignMember(existingMember.Id, Role.Developer, now);

        _db.Users.AddRange(owner, existingMember, newMember);
        await _repository.AddAsync(project, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loadedProject = await _repository.GetByIdWithMembersAsync(
            project.Id,
            CancellationToken.None);

        loadedProject.Should().NotBeNull();
        loadedProject!.Members.Should().ContainSingle(member => member.UserId == existingMember.Id);

        loadedProject.AssignMember(newMember.Id, Role.Manager, now.AddMinutes(1));
        var assignedMember = loadedProject.Members.Single(member => member.UserId == newMember.Id);
        await _repository.AddMemberAsync(assignedMember, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedMembers = await _db.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == project.Id)
            .ToListAsync();

        persistedMembers.Should().HaveCount(2);
        persistedMembers.Should().Contain(member =>
            member.UserId == newMember.Id && member.Role == Role.Manager);
    }

    private static User CreateUser(string email, Role role, DateTimeOffset now) =>
        User.Create(email, "hash", "Test", "User", role, now);
}
