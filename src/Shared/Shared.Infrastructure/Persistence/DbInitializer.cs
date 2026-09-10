using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Platform.Workflow;
using Platform.Workflow.Enums;
using Platform.Workflow.ValueObjects;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Identity;
using Shared.Infrastructure.Identity;
using Vendors.Domain.Entities;
using Vendors.Domain.Enums;

namespace Shared.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IBankFieldEncryptor bankFieldEncryptor)
    {
        await context.Database.MigrateAsync();

        // Encrypt any pre-existing plaintext AccountNumber/Iban rows (raw SQL bypasses converters).
        await EncryptLegacyBankFieldsAsync(context, bankFieldEncryptor);

        var acmeTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        if (!await context.Tenants.AnyAsync())
        {
            context.Tenants.Add(new Tenant
            {
                Id = acmeTenantId,
                Name = "Acme Global Corporation",
                Identifier = "acme-corp",
                IsActive = true,
                CreatedBy = "system-seed"
            });
            await context.SaveChangesAsync();
        }

        // Seed runs without an authenticated user (CurrentTenantId == Empty), so tenant
        // global query filters would hide all rows — bypass them for existence checks.
        var tenantIds = await context.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var tenantId in tenantIds)
        {
            var hasVendorWorkflow = await context.WorkflowDefinitions
                .IgnoreQueryFilters()
                .AnyAsync(w =>
                    w.TenantId == tenantId &&
                    w.EntityType == WorkflowEntityType.Vendor &&
                    !w.IsDeleted);

            if (hasVendorWorkflow)
                continue;

            var definition = new WorkflowDefinition(
                tenantId,
                "Standard Vendor Onboarding Workflow",
                "A multi-step approval process for new vendor registration.",
                WorkflowEntityType.Vendor);

            var step1 = definition.AddStep(
                "Risk & Compliance Review",
                "Review the risk profile and compliance posture of the vendor.",
                order: 1,
                isFinal: false,
                Assignment.ToRole("RiskAndCompliance"),
                SlaDefinition.Standard(2));

            var step2 = definition.AddStep(
                "Procurement Final Approval",
                "Final review and approval by Procurement Management.",
                order: 2,
                isFinal: true,
                Assignment.ToRole("ProcurementManager"),
                SlaDefinition.Standard(1));

            definition.AddTransition(step1.Id, step2.Id);
            definition.Publish();
            context.WorkflowDefinitions.Add(definition);
        }

        await context.SaveChangesAsync();

        await EnsureVendorWorkflowTransitionsAsync(context);

        await SeedDemoUsersAsync(context, passwordHasher, acmeTenantId);
        await SeedAcmeDemoVendorsAsync(context, acmeTenantId);
    }

    /// <summary>
    /// Older seeds persisted steps but not transitions (empty WorkflowTransitions).
    /// Idempotent insert: order-1 → order-2 for each vendor onboarding definition.
    /// </summary>
    private static async Task EnsureVendorWorkflowTransitionsAsync(ApplicationDbContext context)
    {
        if (!context.Database.IsRelational())
            return;

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO [WorkflowTransitions] ([Id], [WorkflowDefinitionId], [FromStepId], [ToStepId])
            SELECT NEWID(), s1.[WorkflowDefinitionId], s1.[Id], s2.[Id]
            FROM [WorkflowStepDefinitions] AS s1
            INNER JOIN [WorkflowStepDefinitions] AS s2
                ON s1.[WorkflowDefinitionId] = s2.[WorkflowDefinitionId]
               AND s2.[Order] = 2
            INNER JOIN [WorkflowDefinitions] AS d
                ON d.[Id] = s1.[WorkflowDefinitionId]
            WHERE s1.[Order] = 1
              AND d.[EntityType] = 1
              AND d.[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1 FROM [WorkflowTransitions] AS t
                  WHERE t.[WorkflowDefinitionId] = s1.[WorkflowDefinitionId]);
            """);
    }


    /// <summary>
    /// Reads BankAccounts via ADO (no value converters), encrypts plaintext cells, writes ciphertext back.
    /// </summary>
    public static async Task EncryptLegacyBankFieldsAsync(
        ApplicationDbContext context,
        IBankFieldEncryptor encryptor,
        CancellationToken cancellationToken = default)
    {
        // InMemory / non-relational providers used in unit tests have no Migrate / SQL surface.
        if (!context.Database.IsRelational())
            return;

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var select = connection.CreateCommand();
            select.CommandText = """
                SELECT [Id], [AccountNumber], [Iban]
                FROM [BankAccounts]
                """;

            var rows = new List<(Guid Id, string AccountNumber, string? Iban)>();
            await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetFieldType(0) == typeof(string)
                        ? Guid.Parse(reader.GetString(0))
                        : reader.GetGuid(0);

                    rows.Add((
                        id,
                        reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2)));
                }
            }

            var isSqlite = (context.Database.ProviderName ?? "")
                .Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

            foreach (var (id, accountNumber, iban) in rows)
            {
                var newAccount = encryptor.IsEncryptedPayload(accountNumber)
                    ? accountNumber
                    : encryptor.Encrypt(accountNumber)!;

                string? newIban = iban;
                if (iban is not null && !encryptor.IsEncryptedPayload(iban))
                    newIban = encryptor.Encrypt(iban);

                if (newAccount == accountNumber && newIban == iban)
                    continue;

                await using var update = connection.CreateCommand();
                update.CommandText = """
                    UPDATE [BankAccounts]
                    SET [AccountNumber] = @accountNumber, [Iban] = @iban
                    WHERE [Id] = @id
                    """;

                AddParam(update, "@accountNumber", newAccount);
                AddParam(update, "@iban", (object?)newIban ?? DBNull.Value);
                // Sqlite stores Guids as TEXT; SqlClient expects uniqueidentifier.
                AddParam(update, "@id", isSqlite ? id.ToString() : id);

                await update.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (shouldClose)
                await context.Database.CloseConnectionAsync();
        }
    }

    private static void AddParam(DbCommand command, string name, object value)
    {
        var p = command.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        command.Parameters.Add(p);
    }

    private static async Task SeedDemoUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        Guid tenantId)
    {
        if (await context.Users.IgnoreQueryFilters().AnyAsync())
            return;

        var demoUsers = new (string Email, string Password, string Role, string Name)[]
        {
            ("admin@example.com", "admin123", "Admin", "System Admin"),
            ("procurement@example.com", "proc123", "ProcurementManager", "Procurement User"),
            ("risk@example.com", "risk123", "RiskAndCompliance", "Risk & Compliance User"),
            ("viewer@example.com", "view123", "Viewer", "Read-Only User")
        };

        foreach (var (email, password, role, name) in demoUsers)
        {
            context.Users.Add(new ApplicationUser
            {
                Email = email.ToLowerInvariant(),
                PasswordHash = passwordHasher.Hash(password),
                DisplayName = name,
                Role = role,
                TenantId = tenantId,
                IsActive = true,
                CreatedBy = "system-seed"
            });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Realistic Acme demo vendors. Runs once per tenant — skipped when any vendor already exists.
    /// </summary>
    private static async Task SeedAcmeDemoVendorsAsync(ApplicationDbContext context, Guid tenantId)
    {
        var hasVendors = await context.Vendors
            .IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && !v.IsDeleted);
        if (hasVendors)
            return;

        var definition = await context.WorkflowDefinitions
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(d => d.Steps)
            .Include(d => d.Transitions)
                .ThenInclude(t => t.Conditions)
            .OrderByDescending(d => d.Version)
            .ThenByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(d =>
                d.TenantId == tenantId &&
                d.EntityType == WorkflowEntityType.Vendor &&
                d.IsActive &&
                !d.IsDeleted);

        if (definition is null)
            return;

        if (definition.Transitions.Count == 0)
            throw new InvalidOperationException("Vendor workflow definition has no transitions; cannot seed demo approvals.");

        await using var tx = await context.Database.BeginTransactionAsync();

        var riskRole = Assignment.ToRole("RiskAndCompliance");
        var procurementRole = Assignment.ToRole("ProcurementManager");
        var riskDue = DateTime.UtcNow.AddDays(2);
        var procurementDue = DateTime.UtcNow.AddDays(1);

        AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1001",
            LegalName: "Cedar & Pine Packaging Ltd",
            TradeName: "Cedar Packaging",
            Tax: "GB 238 4410 92",
            Website: "https://www.cedarpinepackaging.co.uk",
            Currency: "GBP",
            Status: VendorStatus.Draft,
            Contact: new DemoContact("Helen Crowe", "Sales Director", "helen.crowe@cedarpinepackaging.co.uk", "+44 161 496 0144"),
            Address: new DemoAddress("18 Whitworth Street", "Manchester", "Greater Manchester", "United Kingdom", "M1 3NR")));

        AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1002",
            LegalName: "Rhine Precision GmbH",
            TradeName: "Rhine Precision",
            Tax: "DE813472901",
            Website: "https://www.rhine-precision.de",
            Currency: "EUR",
            Status: VendorStatus.Draft,
            Contact: new DemoContact("Lukas Weber", "Key Account Manager", "lukas.weber@rhine-precision.de", "+49 211 4377 620"),
            Address: new DemoAddress("Königsallee 92", "Düsseldorf", "North Rhine-Westphalia", "Germany", "40212")));

        var harbor = AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1003",
            LegalName: "Harborline Logistics Inc",
            TradeName: "Harborline",
            Tax: "94-2183371",
            Website: "https://www.harborlinelogistics.com",
            Currency: "USD",
            Status: VendorStatus.PendingReview,
            Contact: new DemoContact("Marcus Ellison", "VP Operations", "marcus.ellison@harborlinelogistics.com", "+1 415 555 0198"),
            Address: new DemoAddress("1200 The Embarcadero, Suite 400", "San Francisco", "California", "United States", "94105")));

        var apex = AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1004",
            LegalName: "Apex Semiconductor Pte. Ltd.",
            TradeName: "Apex Semi",
            Tax: "201734562M",
            Website: "https://www.apexsemi.sg",
            Currency: "SGD",
            Status: VendorStatus.PendingApproval,
            Contact: new DemoContact("Mei Lin Tan", "Commercial Lead", "mei.lin.tan@apexsemi.sg", "+65 6592 4410"),
            Address: new DemoAddress("1 Fusionopolis Place, #12-08", "Singapore", "Singapore", "Singapore", "138522")));

        var shariq = AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1005",
            LegalName: "Shariq Enterprises",
            TradeName: "Shariq Import Export",
            Tax: "27AAPCS9182K1ZV",
            Website: "https://www.shariqenterprises.in",
            Currency: "INR",
            Status: VendorStatus.Active,
            Contact: new DemoContact("Ayaan Qureshi", "Managing Partner", "ayaan.qureshi@shariqenterprises.in", "+91 22 4056 7780"),
            Address: new DemoAddress("412 Maker Chambers V, Nariman Point", "Mumbai", "Maharashtra", "India", "400021"),
            Bank: new DemoBank("HDFC Bank", "Shariq Enterprises", "50200038776124", "HDFCINBB", "INR"),
            Contract: new DemoContract("CNT-SE-2026-014", "General merchandise supply — FY2026", 18_500_000m, "INR", "Net 45")));

        var northstar = AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1006",
            LegalName: "Northstar Industrial Supply Co.",
            TradeName: "Northstar MRO",
            Tax: "36-4128890",
            Website: "https://www.northstarindustrial.com",
            Currency: "USD",
            Status: VendorStatus.Active,
            Contact: new DemoContact("Danielle Brooks", "National Accounts", "danielle.brooks@northstarindustrial.com", "+1 312 555 0174"),
            Address: new DemoAddress("233 S Wacker Drive, Floor 18", "Chicago", "Illinois", "United States", "60606"),
            Bank: new DemoBank("JPMorgan Chase", "Northstar Industrial Supply Co.", "883192047711", "CHASUS33", "USD"),
            Contract: new DemoContract("CNT-NS-2025-088", "MRO catalog agreement — Midwest plants", 2_450_000m, "USD", "Net 30")));

        var brightwell = AddDemoVendor(context, tenantId, new DemoVendor(
            Number: "V-ACM-1007",
            LegalName: "Brightwell Facilities Ltd",
            TradeName: "Brightwell FM",
            Tax: "GB 704 8821 55",
            Website: "https://www.brightwellfacilities.co.uk",
            Currency: "GBP",
            Status: VendorStatus.Draft,
            Contact: new DemoContact("Owen Gallagher", "Contracts Manager", "owen.gallagher@brightwellfacilities.co.uk", "+44 20 7946 0312"),
            Address: new DemoAddress("77 Marsh Wall", "London", "Greater London", "United Kingdom", "E14 9SH")));

        await context.SaveChangesAsync();

        context.WorkflowInstances.Add(
            Cleared(StartAtRiskReview(definition, tenantId, harbor.Id, "procurement@example.com", riskRole, riskDue)));

        var apexInstance = StartAtRiskReview(definition, tenantId, apex.Id, "procurement@example.com", riskRole, riskDue);
        var apexRiskTask = apexInstance.Tasks.Single(t => t.Status == WorkflowState.InProgress);
        apexInstance.ApproveTask(
            apexRiskTask.Id,
            "risk@example.com",
            "Sanctions screening and beneficial-ownership checks completed. No material findings — move to Procurement.",
            definition,
            procurementRole,
            procurementDue);
        context.WorkflowInstances.Add(Cleared(apexInstance));

        context.WorkflowInstances.Add(Cleared(
            CompleteApproved(definition, tenantId, shariq.Id, riskRole, procurementRole, riskDue, procurementDue)));
        context.WorkflowInstances.Add(Cleared(
            CompleteApproved(definition, tenantId, northstar.Id, riskRole, procurementRole, riskDue, procurementDue)));

        var rejected = StartAtRiskReview(definition, tenantId, brightwell.Id, "procurement@example.com", riskRole, riskDue);
        var rejectedTask = rejected.Tasks.Single(t => t.Status == WorkflowState.InProgress);
        rejected.RejectTask(
            rejectedTask.Id,
            "risk@example.com",
            "Beneficial ownership could not be verified against the supplied registration documents, and the latest financial statements are more than 18 months old. Please resubmit with a current KYC pack and audited accounts.");
        context.WorkflowInstances.Add(Cleared(rejected));

        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static Vendor AddDemoVendor(ApplicationDbContext context, Guid tenantId, DemoVendor spec)
    {
        var vendor = new Vendor
        {
            TenantId = tenantId,
            VendorNumber = spec.Number,
            LegalName = spec.LegalName,
            TradeName = spec.TradeName,
            TaxRegistrationNumber = spec.Tax,
            Website = spec.Website,
            CurrencyCode = spec.Currency,
            Status = spec.Status,
            CreatedBy = "system-seed"
        };

        vendor.Contacts.Add(new VendorContact
        {
            TenantId = tenantId,
            Name = spec.Contact.Name,
            JobTitle = spec.Contact.JobTitle,
            Email = spec.Contact.Email,
            Phone = spec.Contact.Phone,
            ContactType = ContactType.Management,
            IsPrimary = true,
            IsActive = true,
            CreatedBy = "system-seed"
        });

        vendor.Addresses.Add(new VendorAddress
        {
            TenantId = tenantId,
            AddressType = AddressType.HeadOffice,
            AddressLine1 = spec.Address.Line1,
            City = spec.Address.City,
            StateProvince = spec.Address.State,
            Country = spec.Address.Country,
            PostalCode = spec.Address.Postal,
            CreatedBy = "system-seed"
        });

        if (spec.Bank is not null)
        {
            vendor.BankAccounts.Add(new BankAccount
            {
                TenantId = tenantId,
                BankName = spec.Bank.BankName,
                AccountName = spec.Bank.AccountName,
                AccountNumber = spec.Bank.AccountNumber,
                SwiftBic = spec.Bank.Swift,
                CurrencyCode = spec.Bank.Currency,
                IsVerified = true,
                IsApproved = true,
                CreatedBy = "system-seed"
            });
        }

        if (spec.Contract is not null)
        {
            vendor.Contracts.Add(new VendorContract
            {
                TenantId = tenantId,
                ContractNumber = spec.Contract.Number,
                Title = spec.Contract.Title,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                ContractValue = spec.Contract.Value,
                CurrencyCode = spec.Contract.Currency,
                PaymentTerms = spec.Contract.PaymentTerms,
                SlaBrief = "Standard onboarding SLA — 2 business days for order acknowledgement.",
                Status = ContractStatus.Active,
                CreatedBy = "system-seed"
            });
        }

        context.Vendors.Add(vendor);
        return vendor;
    }

    private static WorkflowInstance StartAtRiskReview(
        WorkflowDefinition definition,
        Guid tenantId,
        Guid vendorId,
        string requester,
        Assignment riskAssignment,
        DateTime riskDue)
    {
        return WorkflowInstance.Start(
            tenantId,
            definition,
            new EntityReference(WorkflowEntityType.Vendor, vendorId),
            requester,
            new Assignment(riskAssignment.Strategy, riskAssignment.Value),
            riskDue);
    }

    private static WorkflowInstance CompleteApproved(
        WorkflowDefinition definition,
        Guid tenantId,
        Guid vendorId,
        Assignment riskAssignment,
        Assignment procurementAssignment,
        DateTime riskDue,
        DateTime procurementDue)
    {
        var instance = StartAtRiskReview(definition, tenantId, vendorId, "procurement@example.com", riskAssignment, riskDue);
        var riskTask = instance.Tasks.Single(t => t.Status == WorkflowState.InProgress);
        instance.ApproveTask(
            riskTask.Id,
            "risk@example.com",
            "Risk profile is within appetite. Approved for Procurement.",
            definition,
            new Assignment(procurementAssignment.Strategy, procurementAssignment.Value),
            procurementDue);
        var procurementTask = instance.Tasks.Single(t => t.Status == WorkflowState.InProgress);
        instance.ApproveTask(
            procurementTask.Id,
            "admin@example.com",
            "Commercial terms accepted. Vendor activated.",
            definition);
        return instance;
    }

    private static WorkflowInstance Cleared(WorkflowInstance instance)
    {
        instance.ClearDomainEvents();
        return instance;
    }

    private sealed record DemoContact(string Name, string JobTitle, string Email, string Phone);
    private sealed record DemoAddress(string Line1, string City, string State, string Country, string Postal);
    private sealed record DemoBank(string BankName, string AccountName, string AccountNumber, string Swift, string Currency);
    private sealed record DemoContract(string Number, string Title, decimal Value, string Currency, string PaymentTerms);
    private sealed record DemoVendor(
        string Number,
        string LegalName,
        string TradeName,
        string Tax,
        string Website,
        string Currency,
        VendorStatus Status,
        DemoContact Contact,
        DemoAddress Address,
        DemoBank? Bank = null,
        DemoContract? Contract = null);
}
