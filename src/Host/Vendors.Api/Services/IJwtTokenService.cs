using System.Security.Claims;

namespace Vendors.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(IEnumerable<Claim> claims, DateTime expiresUtc);
}
