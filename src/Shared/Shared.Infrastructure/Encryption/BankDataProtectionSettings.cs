namespace Shared.Infrastructure.Encryption;

public sealed class BankDataProtectionSettings
{
    public const string SectionName = "BankDataProtection";

    /// <summary>
    /// Base64-encoded 32-byte AES-256 key. From user-secrets / env — never commit.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;
}
