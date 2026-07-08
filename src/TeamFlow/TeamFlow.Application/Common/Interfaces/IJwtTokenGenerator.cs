namespace TeamFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, string role);
}
