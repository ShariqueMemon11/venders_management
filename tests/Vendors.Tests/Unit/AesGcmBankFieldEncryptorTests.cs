using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Encryption;

namespace Vendors.Tests.Unit;

public class AesGcmBankFieldEncryptorTests
{
    private static AesGcmBankFieldEncryptor CreateEncryptor()
    {
        var settings = new BankDataProtectionSettings
        {
            EncryptionKey = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA="
        };
        return new AesGcmBankFieldEncryptor(settings);
    }

    [Fact]
    public void RoundTrip_EncryptDecrypt_ReturnsOriginalPlaintext()
    {
        var sut = CreateEncryptor();
        const string plaintext = "GB82WEST12345698765432";

        var cipher = sut.Encrypt(plaintext);
        var roundTrip = sut.Decrypt(cipher);

        roundTrip.Should().Be(plaintext);
        cipher.Should().StartWith(AesGcmBankFieldEncryptor.PayloadPrefix);
        cipher.Should().NotContain(plaintext);
    }

    [Fact]
    public void Encrypt_UsesUniqueNonce_SamePlaintextDifferentCiphertext()
    {
        var sut = CreateEncryptor();
        const string plaintext = "12345678";

        var a = sut.Encrypt(plaintext);
        var b = sut.Encrypt(plaintext);

        a.Should().NotBe(b, "fresh random nonce per encryption — fixed IV would be a critical flaw");
        sut.Decrypt(a).Should().Be(plaintext);
        sut.Decrypt(b).Should().Be(plaintext);
    }

    [Fact]
    public void Decrypt_PassesThroughLegacyPlaintext_WithoutPrefix()
    {
        var sut = CreateEncryptor();
        sut.Decrypt("legacy-account-99").Should().Be("legacy-account-99");
        sut.IsEncryptedPayload("legacy-account-99").Should().BeFalse();
    }

    [Fact]
    public void Encrypt_Null_ReturnsNull()
    {
        var sut = CreateEncryptor();
        sut.Encrypt(null).Should().BeNull();
        sut.Decrypt(null).Should().BeNull();
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var sut = CreateEncryptor();
        var cipher = sut.Encrypt("sensitive")!;
        var tampered = cipher[..^4] + "XXXX";

        var act = () => sut.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }
}

public class BankDataProtectionSettingsTests
{
    [Fact]
    public void GetRequired_Throws_WhenKeyMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => config.GetRequiredBankDataProtectionSettings();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EncryptionKey*user-secrets*");
    }

    [Fact]
    public void GetRequired_Throws_WhenKeyWrongLength()
    {
        // 16 bytes Base64 — too short for AES-256
        var shortKey = Convert.ToBase64String(new byte[16]);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BankDataProtection:EncryptionKey"] = shortKey
            })
            .Build();

        var act = () => config.GetRequiredBankDataProtectionSettings();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exactly 32 bytes*");
    }

    [Fact]
    public void GetRequired_AcceptsValid32ByteBase64Key()
    {
        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BankDataProtection:EncryptionKey"] = key
            })
            .Build();

        var settings = config.GetRequiredBankDataProtectionSettings();

        settings.EncryptionKey.Should().Be(key);
    }
}
