namespace Shared.Application.Common.Interfaces;

/// <summary>
/// AES-GCM field encryption for sensitive bank columns. Ciphertext is opaque to SQL.
/// </summary>
public interface IBankFieldEncryptor
{
    /// <summary>Encrypts plaintext to a versioned ciphertext payload. Null stays null.</summary>
    string? Encrypt(string? plaintext);

    /// <summary>
    /// Decrypts a versioned payload. Values without the encrypted prefix are returned as-is
    /// (legacy plaintext during migration). Null stays null.
    /// </summary>
    string? Decrypt(string? ciphertextOrPlaintext);

    /// <summary>True when the stored value is already an encrypted payload.</summary>
    bool IsEncryptedPayload(string? value);
}
