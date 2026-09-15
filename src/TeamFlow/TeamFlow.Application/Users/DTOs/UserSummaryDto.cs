namespace TeamFlow.Application.Users.DTOs;

public record UserSummaryDto(Guid Id, string Email, string FirstName, string LastName, string Role);
