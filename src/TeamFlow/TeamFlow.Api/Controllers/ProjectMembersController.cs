using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Projects.Commands.AssignMember;

namespace TeamFlow.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/members")]
[ApiController]
public sealed class ProjectMembersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AssignMemberResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(
        Guid projectId,
        [FromBody] AssignMemberRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignMemberCommand(projectId, request.UserId, request.ProjectRole);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var failure = ApiErrorMessages.GetFailureMapping(result.Error);

            return Problem(
                statusCode: failure.StatusCode,
                title: failure.Title,
                detail: result.Error);
        }

        var memberUrl = $"/api/v1/projects/{projectId}/members/{result.Value!.MemberId}";

        return Created(memberUrl, result.Value);
    }
}
