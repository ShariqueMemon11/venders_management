using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vendors.Api.Configuration;
using Vendors.Api.Services;

namespace Vendors.Tests.Unit;

public class JwtSettingsTests
{
    private static JwtSettings ValidSettings => new()
    {
        SecretKey = "local-dev-jwt-secret-at-least-32-characters-long",
        Issuer = "https://localhost:5182",
        Audience = "vendors-api"
    };

    [Fact]
    public void GetRequiredJwtSettings_Throws_WhenSecretMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "https://localhost:5182",
                ["JwtSettings:Audience"] = "vendors-api"
            })
            .Build();

        var act = () => config.GetRequiredJwtSettings();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*user-secrets*");
    }

    [Fact]
    public void GetRequiredJwtSettings_Throws_WhenSecretShorterThan32Characters()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "only-16-chars!!", // 16 < 32
                ["JwtSettings:Issuer"] = "https://localhost:5182",
                ["JwtSettings:Audience"] = "vendors-api"
            })
            .Build();

        var act = () => config.GetRequiredJwtSettings();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32 characters*");
    }

    [Fact]
    public void TokenValidation_RejectsWrongIssuer()
    {
        var settings = ValidSettings;
        var tokenService = new JwtTokenService(settings);
        var token = tokenService.CreateToken(
            [new Claim(ClaimTypes.Email, "test@example.com")],
            DateTime.UtcNow.AddMinutes(5));

        var wrongIssuerParams = settings.CreateTokenValidationParameters();
        wrongIssuerParams.ValidIssuer = "https://evil.example";

        var act = () => ValidateToken(token, wrongIssuerParams);

        act.Should().Throw<SecurityTokenInvalidIssuerException>();
    }

    [Fact]
    public void TokenValidation_RejectsWrongAudience()
    {
        var settings = ValidSettings;
        var tokenService = new JwtTokenService(settings);
        var token = tokenService.CreateToken(
            [new Claim(ClaimTypes.Email, "test@example.com")],
            DateTime.UtcNow.AddMinutes(5));

        var wrongAudienceParams = settings.CreateTokenValidationParameters();
        wrongAudienceParams.ValidAudience = "other-api";

        var act = () => ValidateToken(token, wrongAudienceParams);

        act.Should().Throw<SecurityTokenInvalidAudienceException>();
    }

    [Fact]
    public void TokenValidation_AcceptsToken_WithMatchingIssuerAndAudience()
    {
        var settings = ValidSettings;
        var tokenService = new JwtTokenService(settings);
        var token = tokenService.CreateToken(
            [new Claim(ClaimTypes.Email, "test@example.com")],
            DateTime.UtcNow.AddMinutes(5));

        var principal = ValidateToken(token, settings.CreateTokenValidationParameters());

        principal.FindFirstValue(ClaimTypes.Email).Should().Be("test@example.com");
    }

    private static ClaimsPrincipal ValidateToken(string token, TokenValidationParameters parameters)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, parameters, out _);
    }
}
