using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class ChangeTaskStatusCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ChangeTaskStatusCommandHandler _handler;

    public ChangeTaskStatusCommandHandlerTests()
    {
        _handler = new ChangeTaskStatusCommandHandler(
            _currentUserService,
            _projectRepository,
            _taskItemRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldChangeStatusAndSave_WhenCallerIsProjectMember()
    {
        var now = DateTimeOffset.UtcNow;
        var updatedAt = now.AddHours(1);
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        project.AssignMember(memberId, Role.Developer, now);
        var task = TaskItem.Create(project.Id, "Design API", string.Empty, memberId, null, now);

        _currentUserService.UserId.Returns(memberId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);
        _dateTimeProvider.UtcNow.Returns(updatedAt);

        var result = await _handler.Handle(
            new ChangeTaskStatusCommand(project.Id, task.Id, "done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(TaskItemStatus.Done);
        task.UpdatedAt.Should().Be(updatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var command = ValidCommand();
        _projectRepository.GetByIdWithMembersAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await _taskItemRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await AssertChangesWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotProjectMember()
    {
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var command = ValidCommand(project.Id);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        await _taskItemRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await AssertChangesWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, DateTimeOffset.UtcNow);
        var command = ValidCommand(project.Id);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(command.TaskId, Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await AssertChangesWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTaskBelongsToAnotherProject()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        var task = TaskItem.Create(Guid.NewGuid(), "Other task", string.Empty, null, null, now);
        var command = ValidCommand(project.Id, task.Id);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        task.Status.Should().Be(TaskItemStatus.Todo);
        await AssertChangesWereNotSavedAsync();
    }

    private static ChangeTaskStatusCommand ValidCommand(Guid? projectId = null, Guid? taskId = null) =>
        new(projectId ?? Guid.NewGuid(), taskId ?? Guid.NewGuid(), "InProgress");

    private async Task AssertChangesWereNotSavedAsync()
    {
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
