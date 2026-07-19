using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration.Database.ReadServices;

[Collection(IntegrationTestCollection.Name)]
public sealed class ListProjectMembersReadServiceTests : IntegrationTestBase
{
    private readonly TeamFlowWebAppFactory _factory;

    public ListProjectMembersReadServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task ListMembers_ShouldReturnMembersForRequestedProject()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create(
            "member-read-owner@example.com",
            "hash",
            "Alice",
            "Owner",
            Role.Admin,
            now);
        var firstMember = User.Create(
            "member-read-first@example.com",
            "hash",
            "Bob",
            "Manager",
            Role.Manager,
            now);
        var secondMember = User.Create(
            "member-read-second@example.com",
            "hash",
            "Carol",
            "Developer",
            Role.Developer,
            now);
        var project = Project.Create("Member read project", "Description", owner.Id, now);
        project.AssignMember(firstMember.Id, Role.Developer, now.AddMinutes(1));
        project.AssignMember(secondMember.Id, Role.Manager, now.AddMinutes(2));

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        dbContext.Users.AddRange(owner, firstMember, secondMember);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var result = await readService.ListMembersAsync(project.Id, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(member => member.UserId).Should().Equal(firstMember.Id, secondMember.Id);
        result[0].Email.Should().Be(firstMember.Email);
        result[0].FirstName.Should().Be("Bob");
        result[0].LastName.Should().Be("Manager");
        result[0].Role.Should().Be("Manager");
        result[0].ProjectRole.Should().Be("Developer");
        result[0].JoinedAt.Should().Be(now.AddMinutes(1));
        result[1].Role.Should().Be("Developer");
        result[1].ProjectRole.Should().Be("Manager");
    }

    [Fact]
    public async Task ListMembers_ShouldReturnEmptyList_WhenProjectHasNoMembers()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        var result = await readService.ListMembersAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
