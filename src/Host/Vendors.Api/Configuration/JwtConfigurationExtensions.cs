using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vendors.Api.Configuration;

public static class JwtConfigurationExtensions
{
    public static JwtSettings GetRequiredJwtSettings(this IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey is not configured. For local development run:\n" +
                "  dotnet user-secrets set \"JwtSettings:SecretKey\" \"<at-least-32-characters>\" " +
                "--project src/Host/Vendors.Api/Vendors.Api.csproj");
        }

        if (settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey must be at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            throw new InvalidOperationException("JwtSettings:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException("JwtSettings:Audience is required.");
        }

        return settings;
    }

    public static SymmetricSecurityKey GetSigningKey(this JwtSettings settings) =>
        new(Encoding.UTF8.GetBytes(settings.SecretKey));

    public static TokenValidationParameters CreateTokenValidationParameters(this JwtSettings settings) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = settings.GetSigningKey(),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
}
