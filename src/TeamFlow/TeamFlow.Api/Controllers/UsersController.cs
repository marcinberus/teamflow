using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Users.Commands.RegisterUser;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[ApiController]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Register), new { userId = result.UserId }, result);
    }
}
