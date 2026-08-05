using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Commands.ImportProject;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Tests.Unit.Application.Projects;

public sealed class ImportProjectHandlerTests
{
    private readonly IImportManager<ProjectLine> _projectImportManager = Substitute.For<IImportManager<ProjectLine>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_ShouldAddImportedProjectsWithCurrentUserAsOwner()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var command = new ImportProjectCommand(stream, ".csv");
        var projectLines = new[]
        {
            new ProjectLine("Apollo", "Landing mission"),
            new ProjectLine("Orion", "Deep space exploration")
        };
        var handler = CreateHandler();

        _currentUserService.UserId.Returns(userId);
        _dateTimeProvider.UtcNow.Returns(now);
        ConfigureImport(stream, cancellationToken, projectLines);

        var result = await handler.Handle(command, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _projectRepository.Received(1).AddAsync(
            Arg.Is<Project>(project =>
                project.Name == projectLines[0].Name &&
                project.Description == projectLines[0].Description &&
                project.OwnerId == userId &&
                project.CreatedAt == now),
            cancellationToken);
        await _projectRepository.Received(1).AddAsync(
            Arg.Is<Project>(project =>
                project.Name == projectLines[1].Name &&
                project.Description == projectLines[1].Description &&
                project.OwnerId == userId &&
                project.CreatedAt == now),
            cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldSaveChangesAndReturnImportedProjectIds()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        using var stream = new MemoryStream();
        var command = new ImportProjectCommand(stream, ".csv");
        var importedProjectIds = new List<Guid>();
        var handler = CreateHandler();

        ConfigureImport(
            stream,
            cancellationToken,
            new ProjectLine("Apollo", "Landing mission"),
            new ProjectLine("Orion", "Deep space exploration"));
        _projectRepository
            .AddAsync(
                Arg.Do<Project>(project => importedProjectIds.Add(project.Id)),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(command, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProjectIds.Should().Equal(importedProjectIds);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureWithoutImporting_WhenExtensionIsInvalid()
    {
        using var stream = new MemoryStream();
        var command = new ImportProjectCommand(stream, ".txt");
        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(ErrorMessages.InvalidExtension);
        _projectImportManager.DidNotReceive().Import(
            Arg.Any<FileExtension>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
        await _projectRepository.DidNotReceive().AddAsync(
            Arg.Any<Project>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void ConfigureImport(
        Stream stream,
        CancellationToken cancellationToken,
        params ProjectLine[] projectLines)
    {
        _projectImportManager
            .Import(FileExtension.Csv, stream, cancellationToken)
            .Returns(ToAsyncEnumerable(projectLines));
    }

    private static async IAsyncEnumerable<ProjectLine> ToAsyncEnumerable(
        IEnumerable<ProjectLine> projectLines)
    {
        foreach (var projectLine in projectLines)
        {
            yield return projectLine;
            await Task.Yield();
        }
    }

    private ImportProjectHandler CreateHandler()
    {
        return new ImportProjectHandler(
            _projectImportManager,
            _currentUserService,
            _projectRepository,
            _unitOfWork,
            _dateTimeProvider);
    }
}
