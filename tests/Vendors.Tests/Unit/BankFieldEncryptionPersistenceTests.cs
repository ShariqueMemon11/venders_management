using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Encryption;
using Shared.Infrastructure.Persistence;
using Vendors.Domain.Entities;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

/// <summary>
/// Persistence-layer checks: EF round-trip + raw SQL proves the column is not plaintext.
/// Uses Sqlite so we can SELECT the stored column without going through value converters.
/// </summary>
public class BankFieldEncryptionPersistenceTests
{
    private const string TestKeyBase64 = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=";

    [Fact]
    public async Task EfRoundTrip_ReturnsPlaintext_ButRawColumnIsCiphertext()
    {
        const string accountNumber = "998877665544";
        const string iban = "DE89370400440532013000";

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new FakeCurrentUserService
        {
            TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserId = "test@example.com",
            Role = "Admin"
        });
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<FakeCurrentUserService>());
        services.AddSingleton(new BankDataProtectionSettings { EncryptionKey = TestKeyBase64 });
        services.AddSingleton<IBankFieldEncryptor, AesGcmBankFieldEncryptor>();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connection));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var encryptor = scope.ServiceProvider.GetRequiredService<IBankFieldEncryptor>();
        var currentUser = scope.ServiceProvider.GetRequiredService<FakeCurrentUserService>();

        // Avoid tenant filter / SaveChanges tenant fill during EnsureCreated seed.
        currentUser.UserId = null;
        currentUser.Role = null;
        currentUser.TenantId = Guid.Empty;

        await db.Database.EnsureCreatedAsync();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test",
            Identifier = "test",
            CreatedBy = "test"
        });

        var vendorId = Guid.NewGuid();
        db.Vendors.Add(new Vendor
        {
            Id = vendorId,
            TenantId = tenantId,
            VendorNumber = "V-TEST-0001",
            LegalName = "Encrypt Co",
            CurrencyCode = "EUR",
            CreatedBy = "test"
        });

        var bankId = Guid.NewGuid();
        db.BankAccounts.Add(new BankAccount
        {
            Id = bankId,
            TenantId = tenantId,
            VendorId = vendorId,
            BankName = "Test Bank",
            AccountName = "Encrypt Co",
            AccountNumber = accountNumber,
            Iban = iban,
            CurrencyCode = "EUR",
            CreatedBy = "test"
        });

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        currentUser.TenantId = tenantId;
        currentUser.UserId = "test@example.com";
        currentUser.Role = "Admin";

        // EF path: application sees plaintext.
        var loaded = await db.BankAccounts.SingleAsync(b => b.Id == bankId);
        loaded.AccountNumber.Should().Be(accountNumber);
        loaded.Iban.Should().Be(iban);

        // Raw SQL: column must be ciphertext, not the original values.
        // (No WHERE Id= — Sqlite/EF Guid storage formats differ by provider.)
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT AccountNumber, Iban FROM BankAccounts";

            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read().Should().BeTrue();

            var rawAccount = reader.GetString(0);
            var rawIban = reader.GetString(1);

            rawAccount.Should().StartWith(AesGcmBankFieldEncryptor.PayloadPrefix);
            rawAccount.Should().NotContain(accountNumber);
            rawIban.Should().StartWith(AesGcmBankFieldEncryptor.PayloadPrefix);
            rawIban.Should().NotContain(iban);

            encryptor.Decrypt(rawAccount).Should().Be(accountNumber);
            encryptor.Decrypt(rawIban).Should().Be(iban);
        }
    }

    [Fact]
    public async Task EncryptLegacyBankFields_ConvertsPlaintextRows_ViaRawSql()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new FakeCurrentUserService
        {
            TenantId = Guid.Empty,
            UserId = null,
            Role = null
        });
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<FakeCurrentUserService>());
        services.AddSingleton(new BankDataProtectionSettings { EncryptionKey = TestKeyBase64 });
        services.AddSingleton<IBankFieldEncryptor, AesGcmBankFieldEncryptor>();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connection));

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var encryptor = scope.ServiceProvider.GetRequiredService<IBankFieldEncryptor>();

        await db.Database.EnsureCreatedAsync();

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var vendorId = Guid.NewGuid();
        var bankId = Guid.NewGuid();

        // Insert plaintext directly (simulates pre-encryption rows).
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO Tenants (Id, Name, Identifier, IsActive, CreatedAt, IsDeleted)
                VALUES ($tid, 'T', 't', 1, $now, 0);
                INSERT INTO Vendors (Id, TenantId, VendorNumber, LegalName, TradeName, Status, CurrencyCode, CreatedAt, IsDeleted)
                VALUES ($vid, $tid, 'V-LEGACY', 'Legacy', '', 0, 'USD', $now, 0);
                INSERT INTO BankAccounts (Id, TenantId, VendorId, BankName, AccountName, AccountNumber, Iban, CurrencyCode, IsVerified, IsApproved, CreatedAt, IsDeleted)
                VALUES ($bid, $tid, $vid, 'Old Bank', 'Legacy', 'PLAIN-ACC-001', 'GB00PLAIN0000000001', 'USD', 0, 0, $now, 0);
                """;
            insert.Parameters.AddWithValue("$tid", tenantId.ToString());
            insert.Parameters.AddWithValue("$vid", vendorId.ToString());
            insert.Parameters.AddWithValue("$bid", bankId.ToString());
            insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
            await insert.ExecuteNonQueryAsync();
        }

        await DbInitializer.EncryptLegacyBankFieldsAsync(db, encryptor);

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT AccountNumber, Iban FROM BankAccounts";
            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read().Should().BeTrue();
            var rawAccount = reader.GetString(0);
            var rawIban = reader.GetString(1);

            rawAccount.Should().NotBe("PLAIN-ACC-001");
            rawAccount.Should().StartWith("v1.");
            rawIban.Should().NotBe("GB00PLAIN0000000001");
            encryptor.Decrypt(rawAccount).Should().Be("PLAIN-ACC-001");
            encryptor.Decrypt(rawIban).Should().Be("GB00PLAIN0000000001");
        }
    }
}
