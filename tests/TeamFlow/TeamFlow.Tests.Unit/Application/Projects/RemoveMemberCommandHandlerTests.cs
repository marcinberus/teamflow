using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.RemoveMember;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class RemoveMemberCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RemoveMemberCommandHandler _handler;

    public RemoveMemberCommandHandlerTests()
    {
        _handler = new RemoveMemberCommandHandler(
            _currentUserService,
            _projectRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldRemoveMemberAndSave_WhenCurrentUserIsOwner()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        project.AssignMember(memberId, Role.Developer, now);

        _currentUserService.UserId.Returns(ownerId);
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new RemoveMemberCommand(project.Id, memberId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Members.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Admin)]
    public async Task Handle_ShouldRemoveMember_WhenCurrentUserHasManagementRole(Role role)
    {
        var now = DateTimeOffset.UtcNow;
        var memberId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), now);
        project.AssignMember(memberId, Role.Developer, now);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(role.ToString());
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new RemoveMemberCommand(project.Id, memberId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Members.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCurrentUserCannotManageMembers()
    {
        var now = DateTimeOffset.UtcNow;
        var memberId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), now);
        project.AssignMember(memberId, Role.Developer, now);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new RemoveMemberCommand(project.Id, memberId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        project.Members.Should().ContainSingle();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdWithMembersAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _handler.Handle(
            new RemoveMemberCommand(projectId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenMemberDoesNotExist()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var act = () => _handler.Handle(
            new RemoveMemberCommand(project.Id, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTargetUserIsProjectOwner()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var act = () => _handler.Handle(
            new RemoveMemberCommand(project.Id, ownerId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
