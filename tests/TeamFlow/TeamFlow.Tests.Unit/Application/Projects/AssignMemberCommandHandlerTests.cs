using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.AssignMember;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class AssignMemberCommandHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly AssignMemberCommandHandler _handler;

    public AssignMemberCommandHandlerTests()
    {
        _handler = new AssignMemberCommandHandler(
            _currentUserService,
            _projectRepository,
            _userRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ShouldAssignMemberAndSave_WhenCurrentUserIsOwner()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        var user = CreateUser(now);
        var command = new AssignMemberCommand(project.Id, user.Id, "Developer");

        _currentUserService.UserId.Returns(ownerId);
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MemberId.Should().NotBeEmpty();
        project.Members.Should().ContainSingle(member =>
            member.Id == result.Value.MemberId
            && member.UserId == user.Id
            && member.Role == Role.Developer);
        await _projectRepository.Received(1).AddMemberAsync(
            Arg.Is<ProjectMember>(member => member.Id == result.Value.MemberId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAssignMember_WhenCurrentUserIsManager()
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), now);
        var user = CreateUser(now);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Manager));
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(
            new AssignMemberCommand(project.Id, user.Id, "Manager"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Members.Should().ContainSingle(member => member.Role == Role.Manager);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCurrentUserIsNotOwnerOrManager()
    {
        var project = Project.Create(
            "Apollo",
            "Landing mission",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns(nameof(Role.Developer));
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new AssignMemberCommand(project.Id, Guid.NewGuid(), "Developer"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        await _projectRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCurrentUserRoleIsInvalid()
    {
        var project = Project.Create(
            "Apollo",
            "Landing mission",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _currentUserService.UserId.Returns(Guid.NewGuid());
        _currentUserService.Role.Returns("Unknown");
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _handler.Handle(
            new AssignMemberCommand(project.Id, Guid.NewGuid(), "Developer"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        await _userRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, DateTimeOffset.UtcNow);
        var targetUserId = Guid.NewGuid();

        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _userRepository.GetByIdAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _handler.Handle(
            new AssignMemberCommand(project.Id, targetUserId, "Developer"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        await _projectRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenUserIsAlreadyAMember()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", ownerId, now);
        var user = CreateUser(now);
        project.AssignMember(user.Id, Role.Developer, now);

        _currentUserService.UserId.Returns(ownerId);
        _projectRepository.GetByIdWithMembersAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _handler.Handle(
            new AssignMemberCommand(project.Id, user.Id, "Developer"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _projectRepository.DidNotReceive()
            .AddMemberAsync(Arg.Any<ProjectMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User CreateUser(DateTimeOffset now) =>
        User.Create(
            $"member-{Guid.NewGuid():N}@example.com",
            "hash",
            "Team",
            "Member",
            Role.Developer,
            now);
}
