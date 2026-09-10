using Microsoft.EntityFrameworkCore;
using MediatR;
using Vendors.Application.Interfaces; using Shared.Application.Common.Interfaces; using Platform.Workflow.Interfaces;
using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Commands.CreateVendor;
using Vendors.Application.Vendors.Commands.TerminateVendor;
using Vendors.Application.Vendors.Commands.UpdateVendor;
using Vendors.Application.Vendors.Dtos;
using Vendors.Application.Vendors.Queries.GetVendorById;
using Vendors.Application.Vendors.Queries.GetVendorDirectory;
using Vendors.Domain.Entities;
using Vendors.Domain.Enums;

namespace Vendors.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly IVendorsDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public VendorService(
        IVendorsDbContext context,
        IFileStorageService fileStorageService,
        IWorkflowEngineService workflowEngineService,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _workflowEngineService = workflowEngineService;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<Result<IEnumerable<VendorDto>>> GetAllAsync(Guid tenantId)
    {
        var vendors = await _context.Vendors
            .Where(v => v.TenantId == tenantId)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                VendorNumber = v.VendorNumber,
                LegalName = v.LegalName,
                TradeName = v.TradeName,
                Status = v.Status,
                TaxRegistrationNumber = v.TaxRegistrationNumber,
                Website = v.Website,
                CurrencyCode = v.CurrencyCode,
                CreatedAt = v.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        return Result<IEnumerable<VendorDto>>.SuccessResult(vendors);
    }

    public Task<Result<VendorDto>> GetByIdAsync(Guid id, Guid tenantId) =>
        _mediator.Send(new GetVendorByIdQuery(id, tenantId));

    public async Task<Result<VendorDetailsDto>> GetDetailsByIdAsync(Guid id, Guid tenantId)
    {
        var vendor = await _context.Vendors
            .Where(v => v.Id == id && v.TenantId == tenantId)
            .Select(v => new VendorDetailsDto
            {
                Id = v.Id,
                VendorNumber = v.VendorNumber,
                LegalName = v.LegalName,
                TradeName = v.TradeName,
                Status = v.Status,
                TaxRegistrationNumber = v.TaxRegistrationNumber,
                Website = v.Website,
                CurrencyCode = v.CurrencyCode,
                CreatedAt = v.CreatedAt,
                Contacts = v.Contacts.Select(c => new VendorContactDto
                {
                    Id = c.Id,
                    VendorId = c.VendorId,
                    Name = c.Name,
                    JobTitle = c.JobTitle,
                    Email = c.Email,
                    Phone = c.Phone,
                    Mobile = c.Mobile,
                    ContactType = c.ContactType,
                    IsPrimary = c.IsPrimary,
                    IsActive = c.IsActive
                }).ToList(),
                Addresses = v.Addresses.Select(a => new VendorAddressDto
                {
                    Id = a.Id,
                    VendorId = a.VendorId,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    StateProvince = a.StateProvince,
                    Country = a.Country,
                    PostalCode = a.PostalCode
                }).ToList(),
                BankAccounts = v.BankAccounts.Select(b => new BankAccountDto
                {
                    Id = b.Id,
                    VendorId = b.VendorId,
                    BankName = b.BankName,
                    AccountName = b.AccountName,
                    AccountNumber = b.AccountNumber,
                    Iban = b.Iban,
                    SwiftBic = b.SwiftBic,
                    CurrencyCode = b.CurrencyCode,
                    IsVerified = b.IsVerified,
                    IsApproved = b.IsApproved
                }).ToList(),
                Documents = v.Documents.Select(d => new VendorDocumentDto
                {
                    Id = d.Id,
                    VendorId = d.VendorId,
                    Title = d.Title,
                    DocumentType = d.DocumentType,
                    StoragePath = d.StoragePath,
                    OriginalFileName = d.OriginalFileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    ExpirationDate = d.ExpirationDate,
                    Status = d.Status,
                    VerificationComments = d.VerificationComments
                }).ToList(),
                Contracts = v.Contracts.Select(c => new VendorContractDto
                {
                    Id = c.Id,
                    VendorId = c.VendorId,
                    ContractNumber = c.ContractNumber,
                    Title = c.Title,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    ContractValue = c.ContractValue,
                    CurrencyCode = c.CurrencyCode,
                    PaymentTerms = c.PaymentTerms,
                    SlaBrief = c.SlaBrief,
                    Status = c.Status
                }).ToList(),
                Compliances = v.Compliances.Select(c => new VendorComplianceDto
                {
                    Id = c.Id,
                    VendorId = c.VendorId,
                    RequirementName = c.RequirementName,
                    Description = c.Description,
                    Status = c.Status,
                    CheckedAt = c.CheckedAt,
                    CheckedBy = c.CheckedBy,
                    Comments = c.Comments
                }).ToList(),
                Performances = v.Performances.Select(p => new VendorPerformanceDto
                {
                    Id = p.Id,
                    VendorId = p.VendorId,
                    EvaluationPeriod = p.EvaluationPeriod,
                    QualityScore = p.QualityScore,
                    DeliveryScore = p.DeliveryScore,
                    ResponsivenessScore = p.ResponsivenessScore,
                    ComplianceScore = p.ComplianceScore,
                    AverageScore = p.AverageScore,
                    Comments = p.Comments,
                    Evaluator = p.Evaluator
                }).ToList(),
                Risks = v.Risks.Select(r => new VendorRiskDto
                {
                    Id = r.Id,
                    VendorId = r.VendorId,
                    RiskCategory = r.RiskCategory,
                    RiskLevel = r.RiskLevel,
                    RiskDescription = r.RiskDescription,
                    MitigationPlan = r.MitigationPlan,
                    LastReviewDate = r.LastReviewDate
                }).ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (vendor == null)
            return Result<VendorDetailsDto>.FailureResult("Vendor not found.");

        // Field-level security: Viewer sees basic profile only.
        var role = _currentUserService.Role ?? string.Empty;
        if (role.Equals("Viewer", StringComparison.OrdinalIgnoreCase))
        {
            vendor.TaxRegistrationNumber = null;
            vendor.BankAccounts = new List<BankAccountDto>();
            vendor.Contracts = new List<VendorContractDto>();
            vendor.Documents = new List<VendorDocumentDto>();
            vendor.Compliances = new List<VendorComplianceDto>();
            vendor.Performances = new List<VendorPerformanceDto>();
            vendor.Risks = new List<VendorRiskDto>();
            // Keep primary-looking contacts only (name/email/phone public-ish).
            vendor.Contacts = vendor.Contacts.Where(c => c.IsPrimary).ToList();
        }

        return Result<VendorDetailsDto>.SuccessResult(vendor);
    }

    public Task<Result<Guid>> CreateAsync(VendorDto vendorDto, Guid tenantId) =>
        _mediator.Send(new CreateVendorCommand(vendorDto, tenantId));

    public Task<Result<bool>> UpdateAsync(VendorDto vendorDto, Guid tenantId) =>
        _mediator.Send(new UpdateVendorCommand(vendorDto, tenantId));

    public Task<Result<bool>> DeleteAsync(Guid id, Guid tenantId) =>
        // Soft-terminate instead of hard delete for audit retention.
        TerminateAsync(id, tenantId);

    public Task<Result<bool>> TerminateAsync(Guid id, Guid tenantId) =>
        _mediator.Send(new TerminateVendorCommand(id, tenantId));

    // Contacts
    public async Task<Result<Guid>> AddContactAsync(VendorContactDto dto, Guid tenantId)
    {
        if (dto.IsPrimary)
        {
            await ClearOtherPrimaryContactsAsync(dto.VendorId, tenantId, excludeContactId: null);
        }

        var contact = new VendorContact
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            Name = dto.Name,
            JobTitle = dto.JobTitle,
            Email = dto.Email,
            Phone = dto.Phone,
            Mobile = dto.Mobile,
            ContactType = dto.ContactType,
            IsPrimary = dto.IsPrimary,
            IsActive = dto.IsActive
        };

        _context.VendorContacts.Add(contact);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(contact.Id);
    }

    public async Task<Result<bool>> UpdateContactAsync(VendorContactDto dto, Guid tenantId)
    {
        var contact = await _context.VendorContacts
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == tenantId);

        if (contact == null) return Result<bool>.FailureResult("Contact not found.");

        if (dto.IsPrimary)
        {
            await ClearOtherPrimaryContactsAsync(contact.VendorId, tenantId, excludeContactId: contact.Id);
        }

        contact.Name = dto.Name;
        contact.JobTitle = dto.JobTitle;
        contact.Email = dto.Email;
        contact.Phone = dto.Phone;
        contact.Mobile = dto.Mobile;
        contact.ContactType = dto.ContactType;
        contact.IsPrimary = dto.IsPrimary;
        contact.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    private async Task ClearOtherPrimaryContactsAsync(Guid vendorId, Guid tenantId, Guid? excludeContactId)
    {
        var others = await _context.VendorContacts
            .Where(c => c.VendorId == vendorId && c.TenantId == tenantId && c.IsPrimary)
            .Where(c => excludeContactId == null || c.Id != excludeContactId.Value)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsPrimary = false;
        }
    }

    public async Task<Result<bool>> DeleteContactAsync(Guid contactId, Guid tenantId)
    {
        var contact = await _context.VendorContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.TenantId == tenantId);

        if (contact == null) return Result<bool>.FailureResult("Contact not found.");

        _context.VendorContacts.Remove(contact);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Addresses
    public async Task<Result<Guid>> AddAddressAsync(VendorAddressDto dto, Guid tenantId)
    {
        var address = new VendorAddress
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            AddressType = dto.AddressType,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            StateProvince = dto.StateProvince,
            Country = dto.Country,
            PostalCode = dto.PostalCode
        };

        _context.VendorAddresses.Add(address);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(address.Id);
    }

    public async Task<Result<bool>> UpdateAddressAsync(VendorAddressDto dto, Guid tenantId)
    {
        var address = await _context.VendorAddresses
            .FirstOrDefaultAsync(a => a.Id == dto.Id && a.TenantId == tenantId);

        if (address == null) return Result<bool>.FailureResult("Address not found.");

        address.AddressType = dto.AddressType;
        address.AddressLine1 = dto.AddressLine1;
        address.AddressLine2 = dto.AddressLine2;
        address.City = dto.City;
        address.StateProvince = dto.StateProvince;
        address.Country = dto.Country;
        address.PostalCode = dto.PostalCode;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteAddressAsync(Guid addressId, Guid tenantId)
    {
        var address = await _context.VendorAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.TenantId == tenantId);

        if (address == null) return Result<bool>.FailureResult("Address not found.");

        _context.VendorAddresses.Remove(address);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Bank Accounts
    public async Task<Result<Guid>> AddBankAccountAsync(BankAccountDto dto, Guid tenantId)
    {
        var account = new BankAccount
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            BankName = dto.BankName,
            AccountName = dto.AccountName,
            AccountNumber = dto.AccountNumber,
            Iban = dto.Iban,
            SwiftBic = dto.SwiftBic,
            CurrencyCode = dto.CurrencyCode,
            IsVerified = dto.IsVerified,
            IsApproved = dto.IsApproved
        };

        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(account.Id);
    }

    public async Task<Result<bool>> UpdateBankAccountAsync(BankAccountDto dto, Guid tenantId)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(b => b.Id == dto.Id && b.TenantId == tenantId);

        if (account == null) return Result<bool>.FailureResult("Bank account not found.");

        account.BankName = dto.BankName;
        account.AccountName = dto.AccountName;
        account.AccountNumber = dto.AccountNumber;
        account.Iban = dto.Iban;
        account.SwiftBic = dto.SwiftBic;
        account.CurrencyCode = dto.CurrencyCode;
        account.IsVerified = dto.IsVerified;
        account.IsApproved = dto.IsApproved;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteBankAccountAsync(Guid bankAccountId, Guid tenantId)
    {
        var account = await _context.BankAccounts
            .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.TenantId == tenantId);

        if (account == null) return Result<bool>.FailureResult("Bank account not found.");

        _context.BankAccounts.Remove(account);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Documents
    public async Task<Result<Guid>> UploadDocumentAsync(Guid vendorId, string title, DocumentType type, DateTime? expiry, Stream fileStream, string fileName, string contentType, Guid tenantId)
    {
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId && v.TenantId == tenantId);
        if (vendor == null) return Result<Guid>.FailureResult("Vendor not found.");

        try
        {
            var storagePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, contentType);

            var doc = new VendorDocument
            {
                TenantId = tenantId,
                VendorId = vendorId,
                Title = title,
                DocumentType = type,
                StoragePath = storagePath,
                OriginalFileName = fileName,
                ContentType = contentType,
                FileSize = fileStream.Length,
                ExpirationDate = expiry,
                Status = DocumentStatus.PendingVerification
            };

            _context.VendorDocuments.Add(doc);
            await _context.SaveChangesAsync(default);

            return Result<Guid>.SuccessResult(doc.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.FailureResult($"Failed to upload document: {ex.Message}");
        }
    }

    public async Task<Result<(Stream FileStream, string ContentType, string FileName)>> DownloadDocumentAsync(Guid documentId, Guid tenantId)
    {
        var doc = await _context.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (doc == null) return Result<(Stream, string, string)>.FailureResult("Document not found.");

        try
        {
            var fileData = await _fileStorageService.GetFileAsync(doc.StoragePath);
            return Result<(Stream, string, string)>.SuccessResult((fileData.FileStream, doc.ContentType, doc.OriginalFileName));
        }
        catch (Exception ex)
        {
            return Result<(Stream, string, string)>.FailureResult($"Failed to read document: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateDocumentStatusAsync(Guid documentId, DocumentStatus status, string? comments, Guid tenantId)
    {
        var doc = await _context.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (doc == null) return Result<bool>.FailureResult("Document not found.");

        doc.Status = status;
        doc.VerificationComments = comments;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteDocumentAsync(Guid documentId, Guid tenantId)
    {
        var doc = await _context.VendorDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (doc == null) return Result<bool>.FailureResult("Document not found.");

        try
        {
            await _fileStorageService.DeleteFileAsync(doc.StoragePath);
        }
        catch
        {
            // Log warning that physical file cleanup failed, but continue deleting the database record
        }

        _context.VendorDocuments.Remove(doc);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Contracts
    public async Task<Result<Guid>> AddContractAsync(VendorContractDto dto, Guid tenantId)
    {
        var contract = new VendorContract
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            ContractNumber = dto.ContractNumber,
            Title = dto.Title,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ContractValue = dto.ContractValue,
            CurrencyCode = dto.CurrencyCode,
            PaymentTerms = dto.PaymentTerms,
            SlaBrief = dto.SlaBrief,
            Status = dto.Status
        };

        _context.VendorContracts.Add(contract);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(contract.Id);
    }

    public async Task<Result<bool>> UpdateContractAsync(VendorContractDto dto, Guid tenantId)
    {
        var contract = await _context.VendorContracts
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == tenantId);

        if (contract == null) return Result<bool>.FailureResult("Contract not found.");

        contract.ContractNumber = dto.ContractNumber;
        contract.Title = dto.Title;
        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.ContractValue = dto.ContractValue;
        contract.CurrencyCode = dto.CurrencyCode;
        contract.PaymentTerms = dto.PaymentTerms;
        contract.SlaBrief = dto.SlaBrief;
        contract.Status = dto.Status;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteContractAsync(Guid contractId, Guid tenantId)
    {
        var contract = await _context.VendorContracts
            .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId);

        if (contract == null) return Result<bool>.FailureResult("Contract not found.");

        _context.VendorContracts.Remove(contract);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Compliance
    public async Task<Result<Guid>> AddComplianceAsync(VendorComplianceDto dto, Guid tenantId)
    {
        var compliance = new VendorCompliance
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            RequirementName = dto.RequirementName,
            Description = dto.Description,
            Status = dto.Status,
            CheckedAt = dto.CheckedAt,
            CheckedBy = dto.CheckedBy,
            Comments = dto.Comments
        };

        _context.VendorCompliances.Add(compliance);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(compliance.Id);
    }

    public async Task<Result<bool>> UpdateComplianceAsync(VendorComplianceDto dto, Guid tenantId)
    {
        var compliance = await _context.VendorCompliances
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == tenantId);

        if (compliance == null) return Result<bool>.FailureResult("Compliance check not found.");

        compliance.RequirementName = dto.RequirementName;
        compliance.Description = dto.Description;
        compliance.Status = dto.Status;
        compliance.CheckedAt = dto.CheckedAt;
        compliance.CheckedBy = dto.CheckedBy;
        compliance.Comments = dto.Comments;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteComplianceAsync(Guid complianceId, Guid tenantId)
    {
        var compliance = await _context.VendorCompliances
            .FirstOrDefaultAsync(c => c.Id == complianceId && c.TenantId == tenantId);

        if (compliance == null) return Result<bool>.FailureResult("Compliance check not found.");

        _context.VendorCompliances.Remove(compliance);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Performance
    public async Task<Result<Guid>> AddPerformanceAsync(VendorPerformanceDto dto, Guid tenantId)
    {
        var performance = new VendorPerformance
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            EvaluationPeriod = dto.EvaluationPeriod,
            QualityScore = dto.QualityScore,
            DeliveryScore = dto.DeliveryScore,
            ResponsivenessScore = dto.ResponsivenessScore,
            ComplianceScore = dto.ComplianceScore,
            AverageScore = (dto.QualityScore + dto.DeliveryScore + dto.ResponsivenessScore + dto.ComplianceScore) / 4,
            Comments = dto.Comments,
            Evaluator = dto.Evaluator
        };

        _context.VendorPerformances.Add(performance);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(performance.Id);
    }

    public async Task<Result<bool>> UpdatePerformanceAsync(VendorPerformanceDto dto, Guid tenantId)
    {
        var performance = await _context.VendorPerformances
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);

        if (performance == null) return Result<bool>.FailureResult("Performance evaluation not found.");

        performance.EvaluationPeriod = dto.EvaluationPeriod;
        performance.QualityScore = dto.QualityScore;
        performance.DeliveryScore = dto.DeliveryScore;
        performance.ResponsivenessScore = dto.ResponsivenessScore;
        performance.ComplianceScore = dto.ComplianceScore;
        performance.AverageScore = (dto.QualityScore + dto.DeliveryScore + dto.ResponsivenessScore + dto.ComplianceScore) / 4;
        performance.Comments = dto.Comments;
        performance.Evaluator = dto.Evaluator;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeletePerformanceAsync(Guid performanceId, Guid tenantId)
    {
        var performance = await _context.VendorPerformances
            .FirstOrDefaultAsync(p => p.Id == performanceId && p.TenantId == tenantId);

        if (performance == null) return Result<bool>.FailureResult("Performance evaluation not found.");

        _context.VendorPerformances.Remove(performance);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Risk
    public async Task<Result<Guid>> AddRiskAsync(VendorRiskDto dto, Guid tenantId)
    {
        var risk = new VendorRisk
        {
            TenantId = tenantId,
            VendorId = dto.VendorId,
            RiskCategory = dto.RiskCategory,
            RiskLevel = dto.RiskLevel,
            RiskDescription = dto.RiskDescription,
            MitigationPlan = dto.MitigationPlan,
            LastReviewDate = dto.LastReviewDate
        };

        _context.VendorRisks.Add(risk);
        await _context.SaveChangesAsync(default);

        return Result<Guid>.SuccessResult(risk.Id);
    }

    public async Task<Result<bool>> UpdateRiskAsync(VendorRiskDto dto, Guid tenantId)
    {
        var risk = await _context.VendorRisks
            .FirstOrDefaultAsync(r => r.Id == dto.Id && r.TenantId == tenantId);

        if (risk == null) return Result<bool>.FailureResult("Risk assessment not found.");

        risk.RiskCategory = dto.RiskCategory;
        risk.RiskLevel = dto.RiskLevel;
        risk.RiskDescription = dto.RiskDescription;
        risk.MitigationPlan = dto.MitigationPlan;
        risk.LastReviewDate = dto.LastReviewDate;

        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> DeleteRiskAsync(Guid riskId, Guid tenantId)
    {
        var risk = await _context.VendorRisks
            .FirstOrDefaultAsync(r => r.Id == riskId && r.TenantId == tenantId);

        if (risk == null) return Result<bool>.FailureResult("Risk assessment not found.");

        _context.VendorRisks.Remove(risk);
        await _context.SaveChangesAsync(default);
        return Result<bool>.SuccessResult(true);
    }

    // Dashboard & Approvals Implementation
    public Task<Result<PagedResult<VendorDirectoryDto>>> GetPagedVendorsAsync(
        int page,
        int pageSize,
        Guid tenantId,
        string? name = null,
        VendorStatus? status = null,
        string? country = null) =>
        _mediator.Send(new GetVendorDirectoryQuery(tenantId, page, pageSize, name, status, country));

    public async Task<Result<VendorDashboardDto>> GetDashboardSummaryAsync(Guid tenantId)
    {
        var totalVendors = await _context.Vendors.CountAsync(v => v.TenantId == tenantId);
        var activeVendors = await _context.Vendors.CountAsync(v => v.TenantId == tenantId && v.Status == VendorStatus.Active);
        var pendingApproval = await _context.Vendors.CountAsync(v => v.TenantId == tenantId && 
            (v.Status == VendorStatus.PendingApproval || v.Status == VendorStatus.PendingReview || v.Status == VendorStatus.UnderVerification));
        var draftVendors = await _context.Vendors.CountAsync(v => v.TenantId == tenantId && v.Status == VendorStatus.Draft);
        var terminatedVendors = await _context.Vendors.CountAsync(v => v.TenantId == tenantId && v.Status == VendorStatus.Terminated);

        var totalContractValue = await _context.VendorContracts
            .Where(c => c.TenantId == tenantId && c.Status == ContractStatus.Active)
            .SumAsync(c => c.ContractValue);

        var totalComplianceIssues = await _context.VendorCompliances
            .CountAsync(c => c.TenantId == tenantId && c.Status == ComplianceStatus.NonCompliant);

        var dashboard = new VendorDashboardDto
        {
            TotalVendors = totalVendors,
            ActiveVendors = activeVendors,
            PendingApprovalVendors = pendingApproval,
            DraftVendors = draftVendors,
            TerminatedVendors = terminatedVendors,
            TotalComplianceIssues = totalComplianceIssues,
            TotalContractValue = totalContractValue
        };

        // 1. Pending Approvals Queue (Workflow Requests)
        var pendingInstances = await _context.WorkflowInstances
            .Where(w => w.TenantId == tenantId && w.CurrentState == Platform.Workflow.Enums.WorkflowState.InProgress)
            .ToListAsync();

        var pendingVendorIds = pendingInstances.Select(w => w.TargetEntity.Id).Distinct().ToList();
        var pendingVendors = await _context.Vendors.Where(v => pendingVendorIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);

        var pendingRequests = pendingInstances.Select(w => 
        {
            var vendor = pendingVendors.GetValueOrDefault(w.TargetEntity.Id);
            return new PendingApprovalItemDto
            {
                Id = w.TargetEntity.Id,
                Type = "VendorRegistration",
                ItemName = vendor?.LegalName ?? "Unknown Vendor",
                Detail = "Awaiting Action",
                SubmittedAt = w.CreatedAt
            };
        }).ToList();

        // 1b. Add Vendors manually set to PendingApproval that don't have an active workflow request
        // (To avoid confusion if user manually switches status on Edit screen)
        var manualPendingVendors = await _context.Vendors
            .Where(v => v.TenantId == tenantId && 
                        (v.Status == VendorStatus.PendingApproval || v.Status == VendorStatus.PendingReview || v.Status == VendorStatus.UnderVerification) && 
                        !_context.WorkflowInstances.Any(w => w.TargetEntity.Id == v.Id && w.CurrentState == Platform.Workflow.Enums.WorkflowState.InProgress))
            .Select(v => new PendingApprovalItemDto
            {
                Id = v.Id, // We use VendorId here, so approving it might need special handling if we click on it, but it shows in the list.
                Type = "Manual Pending Status",
                ItemName = v.LegalName,
                Detail = "Vendor status manually set to Pending Approval.",
                SubmittedAt = DateTime.UtcNow
            })
            .ToListAsync();

        dashboard.PendingApprovals.AddRange(pendingRequests);
        dashboard.PendingApprovals.AddRange(manualPendingVendors);
        dashboard.PendingApprovals = dashboard.PendingApprovals
            .OrderByDescending(a => a.SubmittedAt)
            .ToList();

        // 2. Performance Leaderboard (Top 5)
        dashboard.PerformanceLeaderboard = await _context.Vendors
            .Where(v => v.TenantId == tenantId)
            .Select(v => new PerformanceLeaderboardItemDto
            {
                VendorId = v.Id,
                VendorName = v.LegalName,
                VendorNumber = v.VendorNumber,
                AverageScore = v.Performances.Any() ? v.Performances.Average(p => p.AverageScore) : 0,
                Status = v.Status.ToString()
            })
            .OrderByDescending(l => l.AverageScore)
            .Take(5)
            .ToListAsync();

        // 3. Compliance & Risk Alerts
        var complianceIssues = await _context.VendorCompliances
            .Where(c => c.TenantId == tenantId && c.Status == ComplianceStatus.NonCompliant)
            .Select(c => new ComplianceAlertItemDto
            {
                VendorId = c.VendorId,
                VendorName = c.Vendor.LegalName,
                RequirementName = c.RequirementName,
                IssueType = "Non-Compliant",
                Severity = "High"
            })
            .ToListAsync();

        var riskIssues = await _context.VendorRisks
            .Where(r => r.TenantId == tenantId && (r.RiskLevel == RiskLevel.High || r.RiskLevel == RiskLevel.Critical))
            .Select(r => new ComplianceAlertItemDto
            {
                VendorId = r.VendorId,
                VendorName = r.Vendor.LegalName,
                RequirementName = r.RiskCategory,
                IssueType = $"Risk level: {r.RiskLevel}",
                Severity = r.RiskLevel == RiskLevel.Critical ? "Critical" : "High"
            })
            .ToListAsync();

        dashboard.ComplianceAlerts.AddRange(complianceIssues);
        dashboard.ComplianceAlerts.AddRange(riskIssues);

        return Result<VendorDashboardDto>.SuccessResult(dashboard);
    }

    // Workflow Engine
    public async Task<Result<IEnumerable<VendorRequestDto>>> GetPendingRequestsAsync(Guid tenantId)
    {
        var instances = await _context.WorkflowInstances
            .Where(w => w.TenantId == tenantId && w.CurrentState == Platform.Workflow.Enums.WorkflowState.InProgress)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var vendorIds = instances.Select(i => i.TargetEntity.Id).Distinct().ToList();
        var vendors = await _context.Vendors.Where(v => vendorIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);

        var dtos = instances.Select(w => 
        {
            var vendor = vendors.GetValueOrDefault(w.TargetEntity.Id);
            return new VendorRequestDto
            {
                Id = w.Id,
                VendorId = w.TargetEntity.Id,
                VendorName = vendor?.LegalName ?? "Unknown Vendor",
                RequestNumber = "WFL-" + w.Id.ToString()[..8].ToUpper(),
                RequestType = RequestType.VendorRegistration,
                Status = RequestStatus.UnderReview,
                Title = $"Onboard Vendor: {(vendor?.LegalName ?? "Unknown")}",
                Description = "Workflow in progress",
                Requester = w.CreatedBy ?? "System",
                CreatedAt = w.CreatedAt
            };
        }).ToList();

        return Result<IEnumerable<VendorRequestDto>>.SuccessResult(dtos);
    }

    public async Task<Result<VendorRequestDto>> GetRequestDetailsAsync(Guid requestId, Guid tenantId)
    {
        var req = await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Include(w => w.History)
            .FirstOrDefaultAsync(w => w.Id == requestId && w.TenantId == tenantId);

        if (req == null) return Result<VendorRequestDto>.FailureResult("Request not found.");

        var vendor = await _context.Vendors.FindAsync(req.TargetEntity.Id);
        
        var def = await _context.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.Id == req.WorkflowDefinitionId);

        var dto = new VendorRequestDto
        {
            Id = req.Id,
            VendorId = req.TargetEntity.Id,
            VendorName = vendor?.LegalName ?? "New Vendor",
            RequestNumber = "WFL-" + req.Id.ToString()[..8].ToUpper(),
            RequestType = RequestType.VendorRegistration,
            Status = req.CurrentState == Platform.Workflow.Enums.WorkflowState.Approved ? RequestStatus.Approved : 
                     (req.CurrentState == Platform.Workflow.Enums.WorkflowState.Rejected ? RequestStatus.Rejected : RequestStatus.UnderReview),
            Title = $"Onboard Vendor: {vendor?.LegalName ?? "Unknown"}",
            Description = "Approval workflow execution details",
            Requester = req.CreatedBy ?? "System",
            CreatedAt = req.CreatedAt,
            ApprovalSteps = req.Tasks.Select(t => 
            {
                var stepDef = def?.Steps.FirstOrDefault(s => s.Id == t.StepDefinitionId);
                return new ApprovalStepDto
                {
                    Id = t.Id,
                    StepOrder = stepDef?.Order ?? 0,
                    StepName = stepDef?.Name ?? "Task",
                    RequiredRole = t.Assignment.Value,
                    Status = t.Status == Platform.Workflow.Enums.WorkflowState.InProgress ? ApprovalStatus.Pending : 
                             (t.Status == Platform.Workflow.Enums.WorkflowState.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected),
                    ActionDate = t.CompletedAt,
                    Comments = req.History.FirstOrDefault(h => h.TaskId == t.Id)?.Comment?.Value
                };
            }).OrderBy(a => a.StepOrder).ToList(),
            History = req.History.OrderByDescending(h => h.Timestamp).Select(h => new RequestHistoryDto
            {
                Id = h.Id,
                Timestamp = h.Timestamp,
                Action = h.Action.ToString(),
                Actor = h.Actor,
                PreviousStatus = "WorkflowStep", // Simple mockup
                NewStatus = h.Action.ToString(),
                Notes = h.Comment?.Value
            }).ToList()
        };

        return Result<VendorRequestDto>.SuccessResult(dto);
    }

    public async Task<Result<Guid>> SubmitVendorForApprovalAsync(Guid vendorId, string requester, Guid tenantId)
    {
        var vendor = await _context.Vendors
            .Include(v => v.Contacts)
            .Include(v => v.Addresses)
            .Include(v => v.BankAccounts)
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.Id == vendorId && v.TenantId == tenantId);
        if (vendor == null) return Result<Guid>.FailureResult("Vendor not found.");

        if (vendor.Status != VendorStatus.Draft)
            return Result<Guid>.FailureResult("Only Draft vendors can be submitted for approval.");

        var hasDetails =
            (vendor.Contacts?.Count > 0) ||
            (vendor.Addresses?.Count > 0) ||
            (vendor.BankAccounts?.Count > 0) ||
            (vendor.Documents?.Count > 0);

        if (!hasDetails)
            return Result<Guid>.FailureResult(
                "Cannot submit for approval: add at least one contact, address, bank account, or document first.");

        // Check if there's already an active request
        var activeRequest = await _context.WorkflowInstances
            .FirstOrDefaultAsync(w => w.TargetEntity.Id == vendorId && 
                                      w.TenantId == tenantId && 
                                      (w.CurrentState == Platform.Workflow.Enums.WorkflowState.Draft || w.CurrentState == Platform.Workflow.Enums.WorkflowState.InProgress));
            
        if (activeRequest != null)
            return Result<Guid>.FailureResult("Vendor is already under review.");

        vendor.Status = VendorStatus.PendingReview;
        await _context.SaveChangesAsync(default);

        try
        {
            var workflowInstanceId = await _workflowEngineService.StartWorkflowAsync(
                Platform.Workflow.Enums.WorkflowEntityType.Vendor, 
                vendor.Id, 
                "Registration request submitted", 
                default);

            return Result<Guid>.SuccessResult(workflowInstanceId);
        }
        catch (Exception ex)
        {
            vendor.Status = VendorStatus.Draft;
            await _context.SaveChangesAsync(default);
            return Result<Guid>.FailureResult($"Failed to start workflow: {ex.Message}");
        }
    }

    public async Task<Result<VendorRequestDto>> GetActiveRequestForVendorAsync(Guid vendorId, Guid tenantId)
    {
        var req = await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Include(w => w.History)
            .Where(w => w.TargetEntity.Id == vendorId && w.TenantId == tenantId && w.CurrentState == Platform.Workflow.Enums.WorkflowState.InProgress)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        // Latest rejected instance so the stepper can show the return-to-Draft branch.
        req ??= await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Include(w => w.History)
            .Where(w => w.TargetEntity.Id == vendorId && w.TenantId == tenantId && w.CurrentState == Platform.Workflow.Enums.WorkflowState.Rejected)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        if (req == null) return Result<VendorRequestDto>.FailureResult("No active request found.");

        var vendor = await _context.Vendors.FindAsync(vendorId);

        var def = await _context.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.Id == req.WorkflowDefinitionId);

        var requestStatus =
            req.CurrentState == Platform.Workflow.Enums.WorkflowState.Approved ? RequestStatus.Approved :
            req.CurrentState == Platform.Workflow.Enums.WorkflowState.Rejected ? RequestStatus.Rejected :
            RequestStatus.UnderReview;

        var rejectionReason = req.History
            .Where(h => h.Action == Platform.Workflow.Enums.WorkflowAction.Reject)
            .Select(h => h.Comment?.Value)
            .LastOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var dto = new VendorRequestDto
        {
            Id = req.Id, // We use WorkflowInstance ID
            VendorId = req.TargetEntity.Id,
            VendorName = vendor?.LegalName ?? "",
            RequestNumber = "WFL-" + req.Id.ToString()[..8].ToUpper(),
            RequestType = RequestType.VendorRegistration,
            Status = requestStatus,
            Title = "Onboard Vendor: " + (vendor?.LegalName ?? "Unknown"),
            Description = requestStatus == RequestStatus.Rejected
                ? "Approval workflow rejected — vendor returned to Draft"
                : "Approval workflow in progress",
            Requester = req.CreatedBy ?? "System",
            RejectionReason = rejectionReason,
            CreatedAt = req.CreatedAt,
            ApprovalSteps = req.Tasks.Select(t => 
            {
                var stepDef = def?.Steps.FirstOrDefault(s => s.Id == t.StepDefinitionId);
                return new ApprovalStepDto
                {
                    Id = t.Id,
                    StepOrder = stepDef?.Order ?? 0,
                    StepName = stepDef?.Name ?? "Task",
                    RequiredRole = t.Assignment.Value,
                    Status = t.Status == Platform.Workflow.Enums.WorkflowState.InProgress ? ApprovalStatus.Pending : 
                             (t.Status == Platform.Workflow.Enums.WorkflowState.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected),
                    ActionDate = t.CompletedAt,
                    Comments = req.History.FirstOrDefault(h => h.TaskId == t.Id)?.Comment?.Value
                };
            }).OrderBy(a => a.StepOrder).ToList()
        };

        return Result<VendorRequestDto>.SuccessResult(dto);
    }

    public async Task<Result<bool>> ProcessWorkflowActionAsync(Guid requestId, string action, string actor, string? comments, Guid tenantId)
    {
        var currentTask = await _context.WorkflowInstances
            .Where(w => w.Id == requestId && w.TenantId == tenantId)
            .SelectMany(w => w.Tasks)
            .Where(t => t.Status == Platform.Workflow.Enums.WorkflowState.InProgress)
            .Select(t => new { t.Id, t.StepDefinitionId, AssignmentValue = t.Assignment.Value })
            .FirstOrDefaultAsync();

        if (currentTask == null)
        {
            var exists = await _context.WorkflowInstances.AnyAsync(w => w.Id == requestId && w.TenantId == tenantId);
            if (!exists) return Result<bool>.FailureResult("Workflow instance not found.");
            return Result<bool>.FailureResult("No pending tasks available.");
        }

        var currentUserRole = _currentUserService.Role ?? string.Empty;
        var isAdmin = currentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        // Enforce step assignment role (Admin may override).
        if (!isAdmin &&
            !string.IsNullOrWhiteSpace(currentTask.AssignmentValue) &&
            !currentUserRole.Equals(currentTask.AssignmentValue, StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.FailureResult(
                $"This step requires role '{currentTask.AssignmentValue}'. Your role is '{currentUserRole}'.");
        }

        try
        {
            if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                var instanceMeta = await _context.WorkflowInstances
                    .Where(w => w.Id == requestId)
                    .Select(w => new { w.CreatedBy, w.WorkflowDefinitionId })
                    .FirstAsync();

                var isFinalStep = await _context.WorkflowDefinitions
                    .Where(d => d.Id == instanceMeta.WorkflowDefinitionId)
                    .SelectMany(d => d.Steps)
                    .Where(s => s.Id == currentTask.StepDefinitionId)
                    .Select(s => s.IsFinalStep)
                    .FirstOrDefaultAsync();

                // Separation of Duties: submitter cannot give final approval (Admin override allowed).
                if (isFinalStep &&
                    !isAdmin &&
                    !string.IsNullOrWhiteSpace(instanceMeta.CreatedBy) &&
                    instanceMeta.CreatedBy.Equals(actor, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<bool>.FailureResult(
                        "Separation of duties: you cannot give final approval to a workflow you submitted. Ask another Procurement user or an Admin.");
                }

                await _workflowEngineService.ApproveTaskAsync(requestId, currentTask.Id, comments ?? "", default);

                var updatedInstance = await _context.WorkflowInstances.FindAsync(requestId);
                if (updatedInstance?.CurrentState == Platform.Workflow.Enums.WorkflowState.Approved)
                {
                    var targetEntityId = await _context.WorkflowInstances
                        .Where(w => w.Id == requestId)
                        .Select(w => w.TargetEntity.Id)
                        .FirstAsync();

                    var vendor = await _context.Vendors.FindAsync(targetEntityId);
                    if (vendor != null)
                    {
                        vendor.Status = VendorStatus.Active;
                        await _context.SaveChangesAsync(default);
                    }
                }
            }
            else if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                await _workflowEngineService.RejectTaskAsync(requestId, currentTask.Id, comments ?? "", default);

                var targetEntityId = await _context.WorkflowInstances
                    .Where(w => w.Id == requestId)
                    .Select(w => w.TargetEntity.Id)
                    .FirstAsync();

                var vendor = await _context.Vendors.FindAsync(targetEntityId);
                if (vendor != null)
                {
                    vendor.Status = VendorStatus.Draft;
                    await _context.SaveChangesAsync(default);
                }
            }
            else
            {
                return Result<bool>.FailureResult("Invalid action.");
            }

            return Result<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult(ex.Message);
        }
    }
}




