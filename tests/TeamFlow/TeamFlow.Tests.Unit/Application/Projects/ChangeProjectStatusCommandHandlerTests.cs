using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.ChangeProjectStatus;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class ChangeProjectStatusCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ChangeProjectStatusCommandHandler _handler;

    public ChangeProjectStatusCommandHandlerTests()
    {
        _handler = new ChangeProjectStatusCommandHandler(
            _currentUserService,
            _projectRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldChangeStatusAndSave_WhenCurrentUserIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var updatedAt = now.AddHours(1);
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);

        _currentUserService.UserId.Returns(ownerId);
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _dateTimeProvider.UtcNow.Returns(updatedAt);

        var result = await _handler.Handle(
            new ChangeProjectStatusCommand(projectId, "OnHold"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.OnHold);
        project.UpdatedAt.Should().Be(updatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldChangeStatus_WhenCurrentUserIsAdmin()
    {
        var projectId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Admin));
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _handler.Handle(
            new ChangeProjectStatusCommand(projectId, "Completed"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be(ProjectStatus.Completed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCurrentUserIsNotOwnerOrAdmin()
    {
        var projectId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _handler.Handle(
            new ChangeProjectStatusCommand(projectId, "OnHold"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        project.Status.Should().Be(ProjectStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(
            new ChangeProjectStatusCommand(projectId, "OnHold"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
