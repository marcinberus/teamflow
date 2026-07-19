using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

[Collection(IntegrationTestCollection.Name)]
public sealed class RemoveProjectMemberPersistenceTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;
    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private ProjectRepository _repository = null!;
    private UnitOfWork _unitOfWork = null!;

    public RemoveProjectMemberPersistenceTests(IntegrationTestFixture fixture) : base(fixture)
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
    public async Task RemoveMember_ShouldDeleteTrackedMember_WhenUnitOfWorkSavesChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = CreateUser("remove-member-owner@example.com", Role.Manager, now);
        var member = CreateUser("remove-member-target@example.com", Role.Developer, now);
        var project = Project.Create("Apollo", "Landing mission", owner.Id, now);
        project.AssignMember(member.Id, Role.Developer, now);

        _db.Users.AddRange(owner, member);
        await _repository.AddAsync(project, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loadedProject = await _repository.GetByIdWithMembersAsync(
            project.Id,
            CancellationToken.None);
        loadedProject.Should().NotBeNull();

        loadedProject!.RemoveMember(member.Id);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        _db.ChangeTracker.Clear();

        var persistedMember = await _db.ProjectMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.ProjectId == project.Id && item.UserId == member.Id);

        persistedMember.Should().BeNull();
    }

    private static User CreateUser(string email, Role role, DateTimeOffset now) =>
        User.Create(email, "hash", "Test", "User", role, now);
}
