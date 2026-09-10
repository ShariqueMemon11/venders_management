using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Encryption;

/// <summary>
/// EF Core converter: model = plaintext, provider = AES-GCM ciphertext.
/// </summary>
public sealed class EncryptedBankFieldConverter : ValueConverter<string, string>
{
    public EncryptedBankFieldConverter(IBankFieldEncryptor encryptor)
        : base(
            plaintext => encryptor.Encrypt(plaintext)!,
            ciphertext => encryptor.Decrypt(ciphertext)!)
    {
    }
}

/// <summary>
/// Nullable variant for optional Iban.
/// </summary>
public sealed class EncryptedNullableBankFieldConverter : ValueConverter<string?, string?>
{
    public EncryptedNullableBankFieldConverter(IBankFieldEncryptor encryptor)
        : base(
            plaintext => encryptor.Encrypt(plaintext),
            ciphertext => encryptor.Decrypt(ciphertext))
    {
    }
}
