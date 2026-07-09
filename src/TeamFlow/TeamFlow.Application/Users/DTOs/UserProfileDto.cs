namespace TeamFlow.Application.Users.DTOs;

public record UserProfileDto(Guid Id, string Email, string FirstName, string LastName, string Role, DateTimeOffset CreatedAt);
