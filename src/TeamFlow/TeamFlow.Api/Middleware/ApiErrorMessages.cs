using TeamFlow.Application.Common;

namespace TeamFlow.Api.Middleware;

internal static class ApiErrorMessages
{
    private static readonly IReadOnlyDictionary<string, (int StatusCode, string Title)> FailureMappings =
        new Dictionary<string, (int StatusCode, string Title)>(StringComparer.Ordinal)
        {
            [ErrorMessages.NotFound] = (StatusCodes.Status404NotFound, NotFoundTitle),
            [ErrorMessages.Forbidden] = (StatusCodes.Status403Forbidden, ForbiddenTitle),
            [ErrorMessages.InvalidCredentials] = (StatusCodes.Status401Unauthorized, UnauthorizedTitle),
            [ErrorMessages.EmailAlreadyExists] = (StatusCodes.Status409Conflict, ConflictTitle)
        };

    internal const string ValidationFailedTitle = "Validation failed";
    internal const string ValidationFailedDetail = "One or more validation errors occurred.";
    internal const string NotFoundTitle = "Not found";
    internal const string ForbiddenTitle = "Forbidden";
    internal const string UnauthorizedTitle = "Unauthorized";
    internal const string ConflictTitle = "Conflict";
    internal const string InternalServerErrorTitle = "Internal server error";
    internal const string InternalServerErrorDetail = "An unexpected error occurred.";

    internal static (int StatusCode, string Title) GetFailureMapping(string? error)
    {
        return error is not null && FailureMappings.TryGetValue(error, out var mapping)
            ? mapping
            : (StatusCodes.Status422UnprocessableEntity, ValidationFailedTitle);
    }
}
