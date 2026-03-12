using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BallastLane.Domain.Entities;
using BallastLane.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BallastLane.Infrastructure.Auth;

public sealed class JwtProvider(IConfiguration configuration, IDateTimeProvider clock) : IJwtProvider
{
    private const string SecretKey = "JWT_SECRET";
    private const string ExpiryMinutesKey = "JWT_EXPIRY_MINUTES";
    private const string UserIdClaimType = "userId";

    public int ExpiryMinutes => int.Parse(configuration[ExpiryMinutesKey] ?? "60");

    public string Generate(User user)
    {
        var secret = configuration[SecretKey]
            ?? throw new InvalidOperationException($"Configuration key '{SecretKey}' is not set.");

        var expiryMinutes = ExpiryMinutes;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(UserIdClaimType, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: clock.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
