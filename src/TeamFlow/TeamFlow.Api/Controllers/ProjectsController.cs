using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Projects.Commands.CreateProject;
using TeamFlow.Application.Projects.Commands.UpdateProject;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Queries.GetProject;
using TeamFlow.Application.Projects.Queries.ListProjects;

namespace TeamFlow.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
[ApiController]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ListProjectsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ListProjectsResult>> List(
        [FromQuery] ListProjectsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(projectId), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: ApiErrorMessages.NotFoundTitle,
                detail: result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateProjectResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { projectId = result.Value!.ProjectId },
            result.Value);
    }

    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(projectId, request.Name, request.Description);
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
