using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration.Database.ReadServices;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProjectStatisticsReadServiceTests : IntegrationTestBase
{
    private readonly TeamFlowWebAppFactory _factory;

    public ProjectStatisticsReadServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnAggregatedMetrics()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create("statistics-owner@example.com", "hash", "Alice", "Smith", Role.Manager, now);
        var firstMember = User.Create("statistics-member-one@example.com", "hash", "Bob", "Jones", Role.Developer, now);
        var secondMember = User.Create("statistics-member-two@example.com", "hash", "Carol", "Taylor", Role.Developer, now);
        var project = Project.Create("Statistics project", "Description", owner.Id, now);
        project.AssignMember(firstMember.Id, Role.Developer, now);
        project.AssignMember(secondMember.Id, Role.Developer, now);
        var firstDoneTask = project.AddTask("Done task 1", "Description", null, null, now);
        var secondDoneTask = project.AddTask("Done task 2", "Description", null, null, now);
        var inProgressTask = project.AddTask("In progress task", "Description", null, null, now);
        firstDoneTask.ChangeStatus(TaskItemStatus.Done, now);
        secondDoneTask.ChangeStatus(TaskItemStatus.Done, now);
        inProgressTask.ChangeStatus(TaskItemStatus.InProgress, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        dbContext.Users.AddRange(owner, firstMember, secondMember);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var result = await readService.GetStatisticsAsync(project.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProjectId.Should().Be(project.Id);
        result.TotalTasks.Should().Be(3);
        result.TasksByStatus.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["Todo"] = 0,
            ["InProgress"] = 1,
            ["Verification"] = 0,
            ["Done"] = 2,
            ["Cancelled"] = 0
        });
        result.TotalMembers.Should().Be(2);
        result.CompletionPercentage.Should().Be("66.67");
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        var result = await readService.GetStatisticsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
