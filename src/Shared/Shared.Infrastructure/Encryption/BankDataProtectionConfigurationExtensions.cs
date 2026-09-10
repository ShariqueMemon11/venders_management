using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Encryption;

public static class BankDataProtectionConfigurationExtensions
{
    public const int RequiredKeyByteLength = 32; // AES-256

    public static BankDataProtectionSettings GetRequiredBankDataProtectionSettings(
        this IConfiguration configuration)
    {
        var settings = configuration.GetSection(BankDataProtectionSettings.SectionName)
            .Get<BankDataProtectionSettings>()
            ?? new BankDataProtectionSettings();

        if (string.IsNullOrWhiteSpace(settings.EncryptionKey))
        {
            throw new InvalidOperationException(
                "BankDataProtection:EncryptionKey is not configured. For local development run:\n" +
                "  dotnet user-secrets set \"BankDataProtection:EncryptionKey\" \"<base64-32-bytes>\" " +
                "--project src/Host/Vendors.Api/Vendors.Api.csproj\n" +
                "Generate with: [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }) -as [byte[]])");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(settings.EncryptionKey.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "BankDataProtection:EncryptionKey must be a valid Base64 string encoding exactly 32 bytes (AES-256).",
                ex);
        }

        if (keyBytes.Length != RequiredKeyByteLength)
        {
            throw new InvalidOperationException(
                $"BankDataProtection:EncryptionKey must decode to exactly {RequiredKeyByteLength} bytes (AES-256); " +
                $"got {keyBytes.Length}.");
        }

        // Reject all-zero keys as an obvious misconfiguration.
        if (keyBytes.All(b => b == 0))
        {
            throw new InvalidOperationException(
                "BankDataProtection:EncryptionKey must not be an all-zero key.");
        }

        return settings;
    }

    public static byte[] GetKeyBytes(this BankDataProtectionSettings settings) =>
        Convert.FromBase64String(settings.EncryptionKey.Trim());
}
