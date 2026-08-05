using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Tasks.Commands.ImportTask;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Tests.Unit.Application.Tasks;

public sealed class ImportTaskItemHandlerTests
{
    private readonly IImportManager<TaskItemLine> _taskItemImportManager = Substitute.For<IImportManager<TaskItemLine>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_ShouldAddImportedTasksWithCurrentUserAsAssignee()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(projectId, stream, ".csv");
        var taskItemLines = new[]
        {
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory()),
            new TaskItemLine("Implement API".AsMemory(), "Build endpoints".AsMemory())
        };
        var handler = CreateHandler();

        _currentUserService.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(now);
        ConfigureImport(stream, cancellationToken, taskItemLines);

        var result = await handler.Handle(command, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem =>
                taskItem.ProjectId == projectId
                && taskItem.Title == taskItemLines[0].Title.ToString()
                && taskItem.Description == taskItemLines[0].Description.ToString()
                && taskItem.AssignedUserId == userId
                && taskItem.CreatedAt == now),
            cancellationToken);
        await _taskItemRepository.Received(1).AddAsync(
            Arg.Is<TaskItem>(taskItem =>
                taskItem.ProjectId == projectId
                && taskItem.Title == taskItemLines[1].Title.ToString()
                && taskItem.Description == taskItemLines[1].Description.ToString()
                && taskItem.AssignedUserId == userId
                && taskItem.CreatedAt == now),
            cancellationToken);
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

        ConfigureImport(
            stream,
            cancellationToken,
            new TaskItemLine("Design API".AsMemory(), "Define endpoints".AsMemory()),
            new TaskItemLine("Implement API".AsMemory(), "Build endpoints".AsMemory()));
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
    public async Task Handle_ShouldReturnFailureWithoutImporting_WhenExtensionIsInvalid()
    {
        using var stream = new MemoryStream();
        var command = new ImportTaskItemCommand(Guid.NewGuid(), stream, ".txt");
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.InvalidExtension);
        _taskItemImportManager.DidNotReceive().Import(
            Arg.Any<FileExtension>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
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
            _unitOfWork,
            _dateTimeProvider);
    }
}
