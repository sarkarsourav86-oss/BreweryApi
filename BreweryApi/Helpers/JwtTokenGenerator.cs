using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BreweryApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace BreweryApi.Helpers;

public static class JwtTokenGenerator
{
    public static string GenerateToken(JwtOptions options)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "123"),
            new(JwtRegisteredClaimNames.UniqueName, "demo-user"),
            new(JwtRegisteredClaimNames.Email, "demo@example.com"),
            new("scope", "brewery.read")
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(options.TokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}