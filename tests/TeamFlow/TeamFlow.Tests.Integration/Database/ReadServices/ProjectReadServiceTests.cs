using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Database;

namespace TeamFlow.Tests.Integration.Database.ReadServices;

public sealed class ProjectReadServiceTests : IClassFixture<TeamFlowWebAppFactory>
{
    private readonly TeamFlowWebAppFactory _factory;

    public ProjectReadServiceTests(TeamFlowWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListProjects_Dapper_ShouldReturnCorrectCounts()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create("project-read-owner@example.com", "hash", "Alice", "Smith", Role.Manager, now);
        var member = User.Create("project-read-member@example.com", "hash", "Bob", "Jones", Role.Developer, now);
        var project = Project.Create("Dapper count project", "Description", owner.Id, now);
        project.AssignMember(member.Id, Role.Developer, now);
        project.AddTask("First task", "Description", null, null, now);
        project.AddTask("Second task", "Description", null, null, now);
        project.ChangeStatus(ProjectStatus.OnHold, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        dbContext.Users.AddRange(owner, member);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var (items, totalCount) = await readService.ListProjectsAsync(1, 100, "OnHold", CancellationToken.None);
        var result = items.Single(item => item.Id == project.Id);

        totalCount.Should().BeGreaterThanOrEqualTo(1);
        result.OwnerName.Should().Be("Alice Smith");
        result.TaskCount.Should().Be(2);
        result.MemberCount.Should().Be(1);
        result.Status.Should().Be("OnHold");
    }

    [Fact]
    public async Task ListProjects_Dapper_ShouldReturnRequestedPage()
    {
        var now = DateTimeOffset.UtcNow;
        var owner = User.Create("project-pagination-owner@example.com", "hash", "Alice", "Smith", Role.Manager, now);
        var oldestProject = Project.Create("Pagination oldest project", "Description", owner.Id, now.AddMinutes(-2));
        var middleProject = Project.Create("Pagination middle project", "Description", owner.Id, now.AddMinutes(-1));
        var newestProject = Project.Create("Pagination newest project", "Description", owner.Id, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        var readService = scope.ServiceProvider.GetRequiredService<IProjectReadService>();

        dbContext.Users.Add(owner);
        dbContext.Projects.AddRange(oldestProject, middleProject, newestProject);
        await dbContext.SaveChangesAsync();

        var (firstPage, totalCount) = await readService.ListProjectsAsync(1, 2, "Active", CancellationToken.None);
        var (secondPage, secondPageTotalCount) = await readService.ListProjectsAsync(2, 2, "Active", CancellationToken.None);

        totalCount.Should().Be(3);
        secondPageTotalCount.Should().Be(totalCount);
        firstPage.Select(project => project.Id).Should().Equal(newestProject.Id, middleProject.Id);
        secondPage.Select(project => project.Id).Should().Equal(oldestProject.Id);
    }
}
