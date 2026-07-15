using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.DeleteProject;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class DeleteProjectCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _handler = new DeleteProjectCommandHandler(
            _currentUserService,
            _projectRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProjectAndSave_WhenCurrentUserIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(ownerId);
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _handler.Handle(
            new DeleteProjectCommand(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _projectRepository.Received(1).DeleteAsync(project, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeleteProject_WhenCurrentUserIsAdmin()
    {
        var projectId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Admin));
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var result = await _handler.Handle(
            new DeleteProjectCommand(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _projectRepository.Received(1).DeleteAsync(project, Arg.Any<CancellationToken>());
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
            new DeleteProjectCommand(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        await _projectRepository.DidNotReceive().DeleteAsync(
            Arg.Any<Project>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(
            new DeleteProjectCommand(projectId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await _projectRepository.DidNotReceive().DeleteAsync(
            Arg.Any<Project>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
