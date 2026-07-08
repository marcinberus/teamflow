using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;
using TeamFlow.Infrastructure.Database.Repositories;

namespace TeamFlow.Tests.Integration.Database.Repositories;

public sealed class UserRepositoryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    private TeamFlowDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;
    private UserRepository _repository = null!;

    public UserRepositoryTests(DatabaseFixture fixture)
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
        _repository = new UserRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _db.DisposeAsync();
    }

    private static User CreateUser(string email = "test@example.com", Role role = Role.Developer) =>
        User.Create(email, "hash", "First", "Last", role, DateTimeOffset.UtcNow);

    private async Task SeedAsync(User user)
    {
        await _repository.AddAsync(user, CancellationToken.None);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistUser_WhenSaved()
    {
        var user = CreateUser();

        await _repository.AddAsync(user, CancellationToken.None);
        await _db.SaveChangesAsync();

        var saved = await _db.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
        saved!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task AddAsync_ShouldStoreCorrectProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create("props@example.com", "hashed_pw", "Alice", "Smith", Role.Manager, now);

        await _repository.AddAsync(user, CancellationToken.None);
        await _db.SaveChangesAsync();

        var actual = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        actual.Email.Should().Be("props@example.com");
        actual.PasswordHash.Should().Be("hashed_pw");
        actual.FirstName.Should().Be("Alice");
        actual.LastName.Should().Be("Smith");
        actual.Role.Should().Be(Role.Manager);
        actual.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnTrue_WhenEmailExists()
    {
        var user = CreateUser("exists@example.com");
        await SeedAsync(user);

        var result = await _repository.ExistsByEmailAsync("exists@example.com", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        var result = await _repository.ExistsByEmailAsync("ghost@example.com", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldMatchCaseInsensitively_OnDefaultCollation()
    {
        var user = CreateUser("case@example.com");
        await SeedAsync(user);

        var result = await _repository.ExistsByEmailAsync("CASE@EXAMPLE.COM", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        var user = CreateUser("find@example.com");
        await SeedAsync(user);

        var result = await _repository.GetByEmailAsync("find@example.com", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        var result = await _repository.GetByEmailAsync("nobody@example.com", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenIdExists()
    {
        var user = CreateUser("byid@example.com");
        await SeedAsync(user);

        var result = await _repository.GetByIdAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("byid@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
