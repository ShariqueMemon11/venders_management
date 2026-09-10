// Mint JWTs for claim fail-closed tests (TenantId / Role).
// Usage: dotnet run -- <secret> [mode] [email] [role] [issuer] [audience]
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var secret = args.Length > 0 ? args[0]
    : Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("Pass secret as arg 1 or set JWT_SECRET env var.");
var mode = args.Length > 1 ? args[1] : "no-tenant";
var email = args.Length > 2 ? args[2] : "mint@example.com";
var role = args.Length > 3 ? args[3] : "Admin";
var issuer = args.Length > 4 ? args[4] : "https://localhost:5182";
var audience = args.Length > 5 ? args[5] : "vendors-api";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, email),
    new(ClaimTypes.Email, email),
    new(ClaimTypes.Name, "Minted User"),
};

if (string.Equals(role, "empty", StringComparison.OrdinalIgnoreCase))
{
    claims.Add(new Claim(ClaimTypes.Role, ""));
}
else if (!string.Equals(role, "none", StringComparison.OrdinalIgnoreCase)
         && !string.Equals(role, "no-role", StringComparison.OrdinalIgnoreCase))
{
    claims.Add(new Claim(ClaimTypes.Role, role));
}

foreach (var p in new[]
{
    "Vendor.View", "Vendor.Create", "Vendor.Edit", "Vendor.Submit",
    "Document.Download", "Document.Verify"
})
{
    claims.Add(new Claim("permission", p));
}

if (mode == "with-tenant")
{
    claims.Add(new Claim("TenantId", "00000000-0000-0000-0000-000000000001"));
}
else if (mode == "empty-tenant")
{
    claims.Add(new Claim("TenantId", ""));
}
else if (mode == "bad-tenant")
{
    claims.Add(new Claim("TenantId", "not-a-guid"));
}
else if (mode != "no-tenant" && Guid.TryParse(mode, out _))
{
    claims.Add(new Claim("TenantId", mode));
}

var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddHours(1),
    Issuer = issuer,
    Audience = audience,
    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
});

Console.WriteLine(new JwtSecurityTokenHandler().WriteToken(token));
