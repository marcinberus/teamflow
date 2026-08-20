using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Commands.ImportTask;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class ImportTaskItemHandlerTests
{
    private readonly IImportManager<TaskItemLine> _taskItemImportManager = Substitute.For<IImportManager<TaskItemLine>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_ShouldAddImportedTasksWithProjectMemberAssigneesAndParsedStatuses()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var firstAssignedUserId = Guid.NewGuid();
        var taskItemLines = new[]
        {
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), firstAssignedUserId.ToString().AsMemory(), "2027-05-20T10:00:00+00:00".AsMemory(), "InProgress".AsMemory()),
            new TaskItemLine("Implement API".AsMemory(), "Build endpoints".AsMemory(), "not-a-guid".AsMemory(), "2027-06-21T11:30:00+00:00".AsMemory(), "done".AsMemory())
        };
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, userId, cancellationToken, firstAssignedUserId);
        _dateTimeProvider.UtcNow.Returns(now);
        ConfigureImport(stream, cancellationToken, taskItemLines);

        var result = await handler.Handle(command, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem =>
                taskItem.ProjectId == projectId
                && taskItem.Title == taskItemLines[0].Title.ToString()
                && taskItem.Description == taskItemLines[0].Description.ToString()
                && taskItem.Status == TaskItemStatus.InProgress
                && taskItem.AssignedUserId == firstAssignedUserId
                && taskItem.DueDate == DateTimeOffset.Parse(taskItemLines[0].DueDate.ToString())
                && taskItem.CreatedAt == now),
            cancellationToken);
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem =>
                taskItem.ProjectId == projectId
                && taskItem.Title == taskItemLines[1].Title.ToString()
                && taskItem.Description == taskItemLines[1].Description.ToString()
                && taskItem.Status == TaskItemStatus.Done
                && taskItem.AssignedUserId == userId
                && taskItem.DueDate == DateTimeOffset.Parse(taskItemLines[1].DueDate.ToString())
                && taskItem.CreatedAt == now),
            cancellationToken);
    }

    [Theory]
    [InlineData("not-a-status")]
    [InlineData("99")]
    public async Task Handle_ShouldUseTodoStatus_WhenImportedStatusIsInvalidOrUndefined(string importedStatus)
    {
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, Guid.NewGuid(), CancellationToken.None);
        ConfigureImport(
            stream,
            CancellationToken.None,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), "invalid-user-id".AsMemory(), "2027-05-20".AsMemory(), importedStatus.AsMemory()));

        await handler.Handle(command, CancellationToken.None);

        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem => taskItem.Status == TaskItemStatus.Todo),
            CancellationToken.None);
    }

    [Theory]
    [InlineData("2027-05-20T10:00:00Z", 0)]
    [InlineData("2027-05-20T10:00:00+02:00", 120)]
    public async Task Handle_ShouldParseImportedDueDate(string importedDueDate, int offsetInMinutes)
    {
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var expectedDueDate = new DateTimeOffset(2027, 5, 20, 10, 0, 0, TimeSpan.FromMinutes(offsetInMinutes));
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, Guid.NewGuid(), CancellationToken.None);
        ConfigureImport(
            stream,
            CancellationToken.None,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), "invalid-user-id".AsMemory(), importedDueDate.AsMemory(), "Todo".AsMemory()));

        await handler.Handle(command, CancellationToken.None);

        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem => taskItem.DueDate == expectedDueDate),
            CancellationToken.None);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    public async Task Handle_ShouldUseNullDueDate_WhenImportedDueDateCannotBeParsed(string importedDueDate)
    {
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, Guid.NewGuid(), CancellationToken.None);
        ConfigureImport(
            stream,
            CancellationToken.None,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), "invalid-user-id".AsMemory(), importedDueDate.AsMemory(), "Todo".AsMemory()));

        await handler.Handle(command, CancellationToken.None);

        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem => taskItem.DueDate == null),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesAndReturnImportedTaskIds()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var importedTaskIds = new List<Guid>();
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, Guid.NewGuid(), cancellationToken);
        ConfigureImport(
            stream,
            cancellationToken,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), "invalid-user-id".AsMemory(), "2027-05-20".AsMemory(), "Todo".AsMemory()),
            new TaskItemLine("Implement API".AsMemory(), "Build endpoints".AsMemory(), "invalid-user-id".AsMemory(), "2027-06-21".AsMemory(), "Done".AsMemory()));
        _taskItemRepository
            .AddAsync(
                Arg.Do<TaskItem>(taskItem => importedTaskIds.Add(taskItem.Id)),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(command, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaskItemsIds.Should().Equal(importedTaskIds);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldAssignCurrentUser_WhenImportedUserIsNotProjectMember()
    {
        var importedUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var handler = CreateHandler();

        ConfigureAccessibleProject(projectId, currentUserId, CancellationToken.None);
        ConfigureImport(
            stream,
            CancellationToken.None,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory(), importedUserId.ToString().AsMemory(), "2027-05-20".AsMemory(), "Todo".AsMemory()));

        await handler.Handle(command, CancellationToken.None);

        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem => taskItem.AssignedUserId == currentUserId),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundWithoutImporting_WhenProjectDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var handler = CreateHandler();

        _projectRepository
            .GetByIdWithMembersAsync(projectId, CancellationToken.None)
            .Returns((Project?)null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.NotFound);
        AssertImportWasNotStarted();
        await AssertTasksWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenWithoutImporting_WhenCurrentUserIsNotProjectMember()
    {
        var projectId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var project = Project.Create("Apollo", "Landing mission", Guid.NewGuid(), DateTimeOffset.UtcNow);
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var handler = CreateHandler();

        _currentUserService.UserId.Returns(currentUserId);
        _projectRepository
            .GetByIdWithMembersAsync(projectId, CancellationToken.None)
            .Returns(project);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.Forbidden);
        AssertImportWasNotStarted();
        await AssertTasksWereNotSavedAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureWithoutImporting_WhenExtensionIsInvalid()
    {
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(Guid.NewGuid(), stream, ".txt");
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.InvalidExtension);
        AssertImportWasNotStarted();
        await AssertTasksWereNotSavedAsync();
        await _projectRepository.DidNotReceive().GetByIdWithMembersAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private void ConfigureAccessibleProject(
        Guid projectId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        params Guid[] memberIds)
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create("Apollo", "Landing mission", currentUserId, now);

        foreach (var memberId in memberIds)
        {
            project.AssignMember(memberId, Role.Developer, now);
        }

        _currentUserService.UserId.Returns(currentUserId);
        _projectRepository
            .GetByIdWithMembersAsync(projectId, cancellationToken)
            .Returns(project);
    }

    private void AssertImportWasNotStarted()
    {
        _taskItemImportManager.DidNotReceive().Import(
            Arg.Any<FileExtension>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    private async Task AssertTasksWereNotSavedAsync()
    {
        await _taskItemRepository.DidNotReceive().AddAsync(
            Arg.Any<TaskItem>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void ConfigureImport(
        Stream stream,
        CancellationToken cancellationToken,
        params TaskItemLine[] taskItemLines)
    {
        _taskItemImportManager
            .Import(FileExtension.Csv, stream, cancellationToken)
            .Returns(ToAsyncEnumerable(taskItemLines));
    }

    private static async IAsyncEnumerable<TaskItemLine> ToAsyncEnumerable(
        IEnumerable<TaskItemLine> taskItemLines)
    {
        foreach (var taskItemLine in taskItemLines)
        {
            yield return taskItemLine;
            await Task.Yield();
        }
    }

    private ImportTaskItemHandler CreateHandler()
    {
        return new ImportTaskItemHandler(
            _taskItemImportManager,
            _currentUserService,
            _taskItemRepository,
            _projectRepository,
            _unitOfWork,
            _dateTimeProvider,
            Substitute.For<ILogger<ImportTaskItemHandler>>());
    }
}
