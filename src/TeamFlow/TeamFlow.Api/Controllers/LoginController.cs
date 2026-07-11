using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Users.Commands.LoginUser;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/login")]
[ApiController]
public sealed class LoginController : ControllerBase
{
    private readonly IMediator _mediator;

    public LoginController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LoginUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var failure = ApiErrorMessages.GetFailureMapping(result.Error);

            return Problem(
                statusCode: failure.StatusCode,
                title: failure.Title,
                detail: result.Error);
        }

        return Ok(result.Value);
    }
}
