using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Commands.UpdateTask;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class UpdateTaskCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly UpdateTaskCommandHandler _handler;

    public UpdateTaskCommandHandlerTests()
    {
        _handler = new UpdateTaskCommandHandler(
            _currentUserService,
            _projectRepository,
            _taskItemRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSaveTask_WhenCallerAndAssigneeBelongToProject()
    {
        var now = DateTimeOffset.UtcNow;
        var updatedAt = now.AddHours(1);
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        project.AssignMember(callerId, Role.Developer, now);
        var task = TaskItem.Create(
            project.Id,
            "Design API",
            "Define endpoints",
            callerId,
            now.AddDays(1),
            now);
        var dueDate = now.AddDays(2);
        var command = new UpdateTaskCommand(
            project.Id,
            task.Id,
            "Design REST API",
            "Define REST endpoints",
            ownerId,
            dueDate);

        _currentUserService.UserId.Returns(callerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);
        _dateTimeProvider.UtcNow.Returns(updatedAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Design REST API");
        task.Description.Should().Be("Define REST endpoints");
        task.AssignedUserId.Should().Be(ownerId);
        task.DueDate.Should().Be(dueDate);
        task.UpdatedAt.Should().Be(updatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldClearAssignment_WhenAssignedUserIsNull()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        var task = TaskItem.Create(project.Id, "Design API", string.Empty, ownerId, null, now);

        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.Handle(
            new UpdateTaskCommand(project.Id, task.Id, "Design API", string.Empty, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        task.AssignedUserId.Should().BeNull();
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
        task.Title.Should().Be("Other task");
        await AssertChangesWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenAssigneeIsNotProjectMember()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        var task = TaskItem.Create(project.Id, "Design API", string.Empty, ownerId, null, now);
        var command = new UpdateTaskCommand(
            project.Id,
            task.Id,
            "Design REST API",
            string.Empty,
            Guid.NewGuid(),
            null);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _taskItemRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.AssignedUserNotProjectMember);
        task.Title.Should().Be("Design API");
        task.AssignedUserId.Should().Be(ownerId);
        await AssertChangesWereNotSavedAsync();
    }

    private static UpdateTaskCommand ValidCommand(Guid? projectId = null, Guid? taskId = null) =>
        new(
            projectId ?? Guid.NewGuid(),
            taskId ?? Guid.NewGuid(),
            "Design REST API",
            "Define REST endpoints",
            null,
            null);

    private async Task AssertChangesWereNotSavedAsync()
    {
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
