using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;
using TeamFlow.Application.Tasks.Commands.CreateTask;
using TeamFlow.Application.Tasks.Commands.UpdateTask;
using TeamFlow.Application.Tasks.Queries.ListTasks;

namespace TeamFlow.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tasks")]
[ApiController]
public sealed class TasksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ListTasksResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ListTasksResult>> List(
        Guid projectId,
        CancellationToken cancellationToken,
        [FromQuery] string? status = null,
        [FromQuery] Guid? assignedUserId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new ListTasksQuery(projectId, status, assignedUserId, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateTaskResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid projectId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(
            projectId,
            request.Title,
            request.Description,
            request.AssignedUserId,
            request.DueDate);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var failure = ApiErrorMessages.GetFailureMapping(result.Error);

            return Problem(
                statusCode: failure.StatusCode,
                title: failure.Title,
                detail: result.Error);
        }

        var taskUrl = $"/api/v1/projects/{projectId}/tasks/{result.Value!.TaskId}";

        return Created(taskUrl, result.Value);
    }

    [HttpPut("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid projectId,
        Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTaskCommand(
            projectId,
            taskId,
            request.Title,
            request.Description,
            request.AssignedUserId,
            request.DueDate);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var failure = ApiErrorMessages.GetFailureMapping(result.Error);

            return Problem(
                statusCode: failure.StatusCode,
                title: failure.Title,
                detail: result.Error);
        }

        return NoContent();
    }

    [HttpPatch("{taskId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(
        Guid projectId,
        Guid taskId,
        [FromBody] ChangeTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeTaskStatusCommand(projectId, taskId, request.Status);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var failure = ApiErrorMessages.GetFailureMapping(result.Error);

            return Problem(
                statusCode: failure.StatusCode,
                title: failure.Title,
                detail: result.Error);
        }

        return NoContent();
    }
}
