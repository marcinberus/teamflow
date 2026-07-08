using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Api.Middleware;

public static class ExceptionHandlingExtensions
{
    public static WebApplication UseExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                if (exception is null)
                    return;

                switch (exception)
                {
                    case ValidationException valdiationException:
                        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
                        {
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Title = ApiErrorMessages.ValidationFailedTitle,
                            Detail = ApiErrorMessages.ValidationFailedDetail,
                            Errors = valdiationException.Errors
                                .GroupBy(e => e.PropertyName)
                                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                        }, context.RequestAborted);
                        return;

                    case NotFoundException notFoundException:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status404NotFound,
                            Title = ApiErrorMessages.NotFoundTitle,
                            Detail = notFoundException.Message
                        }, context.RequestAborted);
                        return;

                    case UnauthorizedException unauthorizedException:
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = ApiErrorMessages.UnauthorizedTitle,
                            Detail = unauthorizedException.Message
                        }, context.RequestAborted);
                        return;

                    case ForbiddenException forbiddenException:
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = ApiErrorMessages.ForbiddenTitle,
                            Detail = forbiddenException.Message
                        }, context.RequestAborted);
                        return;

                    case ConflictException conflictException:
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status409Conflict,
                            Title = ApiErrorMessages.ConflictTitle,
                            Detail = conflictException.Message
                        }, context.RequestAborted);
                        return;

                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Status = StatusCodes.Status500InternalServerError,
                            Title = ApiErrorMessages.InternalServerErrorTitle,
                            Detail = ApiErrorMessages.InternalServerErrorDetail
                        }, context.RequestAborted);
                        return;
                }
            });
        });

        return app;
    }
}
