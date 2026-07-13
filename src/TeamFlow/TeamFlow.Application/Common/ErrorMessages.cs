namespace TeamFlow.Application.Common;

public static class ErrorMessages
{
    public const string NotFound = "The requested resource was not found.";
    public const string Forbidden = "You do not have permission to perform this action.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string EmailAlreadyExists = "A user with this email address already exists.";
    public const string InvalidRole = "The specified role is invalid.";
}
