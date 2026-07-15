using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class CreateTaskCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _handler = new CreateTaskCommandHandler(
            _currentUserService,
            _projectRepository,
            _taskItemRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldCreateAndSaveTask_WhenCallerIsOwnerAndAssigneeIsMember()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        project.AssignMember(assigneeId, Role.Developer, now);
        var command = new CreateTaskCommand(
            project.Id,
            "Design API",
            "Define endpoints",
            assigneeId,
            now.AddDays(1));

        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaskId.Should().NotBeEmpty();
        project.Tasks.Should().ContainSingle(task =>
            task.Id == result.Value.TaskId
            && task.ProjectId == project.Id
            && task.AssignedUserId == assigneeId
            && task.Status == TaskItemStatus.Todo
            && task.CreatedAt == now);
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(task => task.Id == result.Value.TaskId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateTask_WhenCallerIsAssignedMember()
    {
        var now = DateTimeOffset.UtcNow;
        var memberId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), now);
        project.AssignMember(memberId, Role.Developer, now);

        _currentUserService.UserId.Returns(memberId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await _handler.Handle(
            new CreateTaskCommand(project.Id, "Design API", "Define endpoints", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Tasks.Should().ContainSingle(task => task.AssignedUserId == memberId);
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(task => task.AssignedUserId == memberId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdWithMembersAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(
            new CreateTaskCommand(projectId, "Design API", string.Empty, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await AssertTaskWasNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotProjectMember()
    {
        var project = Project.Create(
            "Apollo",
            "Landing mission",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new CreateTaskCommand(project.Id, "Design API", string.Empty, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        project.Tasks.Should().BeEmpty();
        await AssertTaskWasNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenAssigneeIsNotProjectMember()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create(
            "Apollo",
            "Landing mission",
            ownerId,
            DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new CreateTaskCommand(project.Id, "Design API", string.Empty, Guid.NewGuid(), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.AssignedUserNotProjectMember);
        project.Tasks.Should().BeEmpty();
        await AssertTaskWasNotSavedAsync();
    }

    private async Task AssertTaskWasNotSavedAsync()
    {
        await _taskItemRepository.DidNotReceive().AddAsync(
            Arg.Any<TaskItem>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
