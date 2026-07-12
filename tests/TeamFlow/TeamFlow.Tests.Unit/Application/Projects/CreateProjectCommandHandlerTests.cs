using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class CreateProjectCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_ShouldAddProjectWithOwnerIdFromCurrentUser()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var command = new CreateProjectCommand("Apollo", "Landing mission");
        var handler = CreateHandler();

        _currentUserService.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProjectId.Should().NotBeEmpty();
        await _projectRepository.Received(1).AddAsync(
            Arg.Is<Project>(project =>
                project.Id == result.Value.ProjectId &&
                project.Name == command.Name &&
                project.Description == command.Description &&
                project.OwnerId == userId &&
                project.CreatedAt == now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSaveChanges_WhenProjectIsCreated()
    {
        var handler = CreateHandler();

        await handler.Handle(new CreateProjectCommand("Apollo", "Landing mission"), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private CreateProjectCommandHandler CreateHandler()
    {
        return new CreateProjectCommandHandler(
            _currentUserService,
            _projectRepository,
            _unitOfWork,
            _dateTimeProvider);
    }
}
