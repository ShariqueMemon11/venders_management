using System.Security.Cryptography;
using System.Text;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Encryption;

/// <summary>
/// AES-256-GCM with a fresh random 12-byte nonce per encryption.
/// Wire format: <c>v1.</c> + Base64(nonce || tag || ciphertext). Never reuses a fixed IV.
/// </summary>
public sealed class AesGcmBankFieldEncryptor : IBankFieldEncryptor
{
    public const string PayloadPrefix = "v1.";

    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmBankFieldEncryptor(BankDataProtectionSettings settings)
    {
        _key = settings.GetKeyBytes();
        if (_key.Length != BankDataProtectionConfigurationExtensions.RequiredKeyByteLength)
        {
            throw new InvalidOperationException(
                $"AES-GCM key must be {BankDataProtectionConfigurationExtensions.RequiredKeyByteLength} bytes.");
        }
    }

    public bool IsEncryptedPayload(string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.StartsWith(PayloadPrefix, StringComparison.Ordinal);

    public string? Encrypt(string? plaintext)
    {
        if (plaintext is null)
            return null;

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

        return PayloadPrefix + Convert.ToBase64String(payload);
    }

    public string? Decrypt(string? ciphertextOrPlaintext)
    {
        if (ciphertextOrPlaintext is null)
            return null;

        // Legacy plaintext (pre-migration) or empty — pass through so migration can re-save.
        if (!IsEncryptedPayload(ciphertextOrPlaintext))
            return ciphertextOrPlaintext;

        var base64 = ciphertextOrPlaintext[PayloadPrefix.Length..];
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Bank field ciphertext is not valid Base64.", ex);
        }

        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Bank field ciphertext is truncated.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintextBytes = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException(
                "Failed to decrypt bank field — wrong key or tampered ciphertext.", ex);
        }

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
