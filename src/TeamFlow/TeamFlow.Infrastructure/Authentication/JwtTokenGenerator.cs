using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider) 
    : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public string GenerateToken(Guid userId, string email, string role)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var expiryMinutes = _configuration.GetValue("Jwt:ExpiryMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(expiryMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
