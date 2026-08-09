using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using TeamFlow.Domain.Exceptions;

namespace TeamFlow.Api.Middleware;

// TODO: supress diagnostic callback at framework exception handler when migrate to .NET10:
// builder.Services.AddExceptionHandler(options =>
// {
//     options.SuppressDiagnosticsCallback = _ => true;
// });
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.exceptionhandleroptions.suppressdiagnosticscallback?view=aspnetcore-10.0
// and remove `"Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "Fatal",`
// from appsettings.json as its current solution for avoid log duplication
public static class ExceptionHandlingExtensions
{
    public static WebApplication UseExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionFeature?.Error;

                if (exception is null)
                {
                    return;
                }

                var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
                var requestPath = exceptionFeature?.Path ?? context.Request.Path.Value;

                var logger = Log
                    .ForContext("SourceContext", "GlobalExceptionHandler")
                    .ForContext("RequestId", context.TraceIdentifier)
                    .ForContext("RequestMethod", context.Request.Method)
                    .ForContext("RequestPath", requestPath);

                switch (exception)
                {
                    case ValidationException validationException:
                        var validationErrors = validationException.Errors.ToArray();
                        logger.Warning("Request validation failed with {ValidationErrorCount} error(s) for {InvalidProperties}",
                            validationErrors.Length,
                            validationErrors
                                .Select(error => error.PropertyName)
                                .Distinct()
                                .ToArray());

                        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
                        {
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Title = ApiErrorMessages.ValidationFailedTitle,
                            Detail = ApiErrorMessages.ValidationFailedDetail,
                            Errors = validationErrors
                                .GroupBy(e => e.PropertyName)
                                .ToDictionary(
                                    g => g.Key,
                                    g => g.Select(e => e.ErrorMessage).ToArray())
                        }, context.RequestAborted);

                        return;

                    case NotFoundException notFoundException:
                        logger.Information("Requested resource was not found: {ErrorMessage}",
                            notFoundException.Message);

                        await WriteProblemAsync(
                            context,
                            StatusCodes.Status404NotFound,
                            ApiErrorMessages.NotFoundTitle,
                            notFoundException.Message);

                        return;

                    case ConflictException conflictException:
                        logger.Warning("Request resulted in a conflict: {ErrorMessage}",
                            conflictException.Message);

                        await WriteProblemAsync(
                            context,
                            StatusCodes.Status409Conflict,
                            ApiErrorMessages.ConflictTitle,
                            conflictException.Message);

                        return;

                    default:
                        logger.Error(exception, "Unhandled exception while processing request");

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
