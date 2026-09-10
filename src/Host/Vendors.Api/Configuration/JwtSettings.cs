namespace Vendors.Api.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>HS256 signing key — must come from user-secrets/env, never committed.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
