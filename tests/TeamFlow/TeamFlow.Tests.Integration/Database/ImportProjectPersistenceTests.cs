using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.ImportProject;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Importing.Projects;
using TeamFlow.Importing.Projects.Importerts;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database;

[Collection(IntegrationTestCollection.Name)]
public sealed class ImportProjectPersistenceTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;

    public ImportProjectPersistenceTests(IntegrationTestFixture fixture) : base(fixture)
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
    }

    public override async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ShouldPersistEveryCsvProjectWithCurrentUserAsOwner()
    {
        var now = new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.Zero);
        var owner = User.Create("import-persistence@example.com", "hash", "Test", "Owner", Role.Developer, now);
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var handler = new ImportProjectHandler(
            new ProjectImportManager([new CsvImporter()]),
            new TestCurrentUserService(owner.Id),
            new ProjectRepository(_db),
            new UnitOfWork(_db),
            new TestDateTimeProvider(now));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "\"Apollo\",\"Landing mission\"\r\n\"Orion\",\"Deep space exploration\""));

        var result = await handler.Handle(
            new ImportProjectCommand(stream, ".csv"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var importedProjectIds = result.Value!.ProjectIds.ToArray();
        importedProjectIds.Should().HaveCount(2);
        _db.ChangeTracker.Clear();

        var projects = await _db.Projects
            .AsNoTracking()
            .Where(project => importedProjectIds.Contains(project.Id))
            .OrderBy(project => project.Name)
            .ToListAsync();

        projects.Should().HaveCount(2);
        projects.Should().AllSatisfy(project =>
        {
            project.OwnerId.Should().Be(owner.Id);
            project.Status.Should().Be(ProjectStatus.Active);
            project.CreatedAt.Should().Be(now);
            project.UpdatedAt.Should().BeNull();
        });
        projects.Select(project => new { project.Name, project.Description }).Should().Equal(
            new { Name = "Apollo", Description = "Landing mission" },
            new { Name = "Orion", Description = "Deep space exploration" });
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
        public string Role => "Developer";
    }

    private sealed class TestDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
