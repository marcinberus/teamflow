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
                {
                    return;
                }

                switch (exception)
                {
                    case ValidationException validationException:
                        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
                        {
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Title = ApiErrorMessages.ValidationFailedTitle,
                            Detail = ApiErrorMessages.ValidationFailedDetail,
                            Errors = validationException.Errors
                                .GroupBy(e => e.PropertyName)
                                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                        }, context.RequestAborted);
                        return;

                    case NotFoundException notFoundException:
                        await WriteProblemAsync(
                            context,
                            StatusCodes.Status404NotFound,
                            ApiErrorMessages.NotFoundTitle,
                            notFoundException.Message);
                        return;

                    case ConflictException conflictException:
                        await WriteProblemAsync(
                            context,
                            StatusCodes.Status409Conflict,
                            ApiErrorMessages.ConflictTitle,
                            conflictException.Message);
                        return;

                    default:
                        await WriteProblemAsync(
                            context,
                            StatusCodes.Status500InternalServerError,
                            ApiErrorMessages.InternalServerErrorTitle,
                            ApiErrorMessages.InternalServerErrorDetail);
                        return;
                }
            });
        });

        return app;
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        }, context.RequestAborted);
    }
}
