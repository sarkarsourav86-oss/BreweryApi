using BreweryApi.Helpers;
using BreweryApi.Models;

var jwtOptions = new JwtOptions
{
    Issuer = "BreweryApi",
    Audience = "BreweryApiClient",
    SecretKey = "super-secret-key-that-is-at-least-32-characters-long",
    TokenExpirationMinutes = 60
};

var token = JwtTokenGenerator.GenerateToken(jwtOptions);
Console.WriteLine(token);