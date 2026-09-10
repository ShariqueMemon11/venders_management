namespace Shared.Infrastructure.Identity;

/// <summary>
/// Constants for login timing normalization (see AuthService).
/// </summary>
public static class AuthTimingConstants
{
    /// <summary>
    /// Valid BCrypt hash (cost factor 12) of the fixed string "timing-side-channel-dummy".
    /// When no user is found, login still verifies against this hash so both failure paths
    /// perform one full BCrypt computation — closes email-enumeration timing side-channels.
    /// </summary>
    public const string DummyPasswordHash =
        "$2a$12$92.xszXgrNXY9AIXGWkyxOB.ElM9lNlKT3Zp6RSI/6QHsjaF2vxPO";
}
