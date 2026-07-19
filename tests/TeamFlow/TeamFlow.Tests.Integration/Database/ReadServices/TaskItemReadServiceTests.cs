using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration.Database.ReadServices;

[Collection(IntegrationTestCollection.Name)]
public sealed class TaskItemReadServiceTests : IntegrationTestBase
{
    private readonly TeamFlowWebAppFactory _factory;

    public TaskItemReadServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task ListTasks_ShouldFilterProjectStatusAndAssignee_AndReturnProjection()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create(
            $"task-read-owner-{Guid.NewGuid():N}@example.com",
            "hash",
            "Project",
            "Owner",
            Role.Manager,
            now);
        var assignee = User.Create(
            $"task-read-assignee-{Guid.NewGuid():N}@example.com",
            "hash",
            "Alice",
            "Smith",
            Role.Developer,
            now);
        var project = Project.Create("Task read project", "Description", owner.Id, now);
        project.AssignMember(assignee.Id, Role.Developer, now);
        var matchingTask = project.AddTask(
            "Design API",
            "Define endpoints",
            assignee.Id,
            now.AddDays(1),
            now.AddMinutes(-2));
        matchingTask.ChangeStatus(TaskItemStatus.InProgress, now);
        project.AddTask("Todo task", "Description", assignee.Id, null, now.AddMinutes(-1));
        project.AddTask("Unassigned task", "Description", null, null, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<ITaskItemReadService>();

        dbContext.Users.AddRange(owner, assignee);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var (items, totalCount) = await readService.ListTasksAsync(
            project.Id,
            "InProgress",
            assignee.Id,
            1,
            20,
            CancellationToken.None);

        totalCount.Should().Be(1);
        var result = items.Should().ContainSingle().Subject;
        result.Id.Should().Be(matchingTask.Id);
        result.Title.Should().Be("Design API");
        result.Description.Should().Be("Define endpoints");
        result.Status.Should().Be("InProgress");
        result.AssignedUserId.Should().Be(assignee.Id);
        result.AssigneeName.Should().Be("Alice Smith");
        result.DueDate.Should().Be(now.AddDays(1));
    }

    [Fact]
    public async Task ListTasks_ShouldReturnRequestedPage_WithTotalCountBeforePagination()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create(
            $"task-page-owner-{Guid.NewGuid():N}@example.com",
            "hash",
            "Page",
            "Owner",
            Role.Manager,
            now);
        var project = Project.Create("Task page project", "Description", owner.Id, now);
        var oldest = project.AddTask("Oldest", string.Empty, null, null, now.AddMinutes(-2));
        var middle = project.AddTask("Middle", string.Empty, null, null, now.AddMinutes(-1));
        var newest = project.AddTask("Newest", string.Empty, null, null, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<ITaskItemReadService>();

        dbContext.Users.Add(owner);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var (firstPage, totalCount) = await readService.ListTasksAsync(
            project.Id,
            null,
            null,
            1,
            2,
            CancellationToken.None);
        var (secondPage, secondTotalCount) = await readService.ListTasksAsync(
            project.Id,
            null,
            null,
            2,
            2,
            CancellationToken.None);

        totalCount.Should().Be(3);
        secondTotalCount.Should().Be(3);
        firstPage.Select(task => task.Id).Should().Equal(newest.Id, middle.Id);
        secondPage.Select(task => task.Id).Should().Equal(oldest.Id);
        secondPage.Single().AssigneeName.Should().BeNull();
    }
}
