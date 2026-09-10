using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Interfaces;

public interface IVendorService
{
    Task<Result<IEnumerable<VendorDto>>> GetAllAsync(Guid tenantId);
    Task<Result<VendorDto>> GetByIdAsync(Guid id, Guid tenantId);
    Task<Result<VendorDetailsDto>> GetDetailsByIdAsync(Guid id, Guid tenantId);
    Task<Result<Guid>> CreateAsync(VendorDto vendorDto, Guid tenantId);
    Task<Result<bool>> UpdateAsync(VendorDto vendorDto, Guid tenantId);
    Task<Result<bool>> DeleteAsync(Guid id, Guid tenantId);
    Task<Result<bool>> TerminateAsync(Guid id, Guid tenantId);

    // Contacts
    Task<Result<Guid>> AddContactAsync(VendorContactDto dto, Guid tenantId);
    Task<Result<bool>> UpdateContactAsync(VendorContactDto dto, Guid tenantId);
    Task<Result<bool>> DeleteContactAsync(Guid contactId, Guid tenantId);

    // Addresses
    Task<Result<Guid>> AddAddressAsync(VendorAddressDto dto, Guid tenantId);
    Task<Result<bool>> UpdateAddressAsync(VendorAddressDto dto, Guid tenantId);
    Task<Result<bool>> DeleteAddressAsync(Guid addressId, Guid tenantId);

    // Bank Accounts
    Task<Result<Guid>> AddBankAccountAsync(BankAccountDto dto, Guid tenantId);
    Task<Result<bool>> UpdateBankAccountAsync(BankAccountDto dto, Guid tenantId);
    Task<Result<bool>> DeleteBankAccountAsync(Guid bankAccountId, Guid tenantId);

    // Documents
    Task<Result<Guid>> UploadDocumentAsync(Guid vendorId, string title, global::Vendors.Domain.Enums.DocumentType type, DateTime? expiry, Stream fileStream, string fileName, string contentType, Guid tenantId);
    Task<Result<(Stream FileStream, string ContentType, string FileName)>> DownloadDocumentAsync(Guid documentId, Guid tenantId);
    Task<Result<bool>> UpdateDocumentStatusAsync(Guid documentId, global::Vendors.Domain.Enums.DocumentStatus status, string? comments, Guid tenantId);
    Task<Result<bool>> DeleteDocumentAsync(Guid documentId, Guid tenantId);

    // Contracts
    Task<Result<Guid>> AddContractAsync(VendorContractDto dto, Guid tenantId);
    Task<Result<bool>> UpdateContractAsync(VendorContractDto dto, Guid tenantId);
    Task<Result<bool>> DeleteContractAsync(Guid contractId, Guid tenantId);

    // Compliance
    Task<Result<Guid>> AddComplianceAsync(VendorComplianceDto dto, Guid tenantId);
    Task<Result<bool>> UpdateComplianceAsync(VendorComplianceDto dto, Guid tenantId);
    Task<Result<bool>> DeleteComplianceAsync(Guid complianceId, Guid tenantId);

    // Performance
    Task<Result<Guid>> AddPerformanceAsync(VendorPerformanceDto dto, Guid tenantId);
    Task<Result<bool>> UpdatePerformanceAsync(VendorPerformanceDto dto, Guid tenantId);
    Task<Result<bool>> DeletePerformanceAsync(Guid performanceId, Guid tenantId);

    // Risk
    Task<Result<Guid>> AddRiskAsync(VendorRiskDto dto, Guid tenantId);
    Task<Result<bool>> UpdateRiskAsync(VendorRiskDto dto, Guid tenantId);
    Task<Result<bool>> DeleteRiskAsync(Guid riskId, Guid tenantId);

    // Directory/Pagination
    Task<Result<PagedResult<VendorDirectoryDto>>> GetPagedVendorsAsync(
        int page,
        int pageSize,
        Guid tenantId,
        string? name = null,
        VendorStatus? status = null,
        string? country = null);

    // Dashboard & Approvals
    Task<Result<VendorDashboardDto>> GetDashboardSummaryAsync(Guid tenantId);
    
    // Workflow Engine
    Task<Result<IEnumerable<VendorRequestDto>>> GetPendingRequestsAsync(Guid tenantId);
    Task<Result<VendorRequestDto>> GetRequestDetailsAsync(Guid requestId, Guid tenantId);
    Task<Result<Guid>> SubmitVendorForApprovalAsync(Guid vendorId, string requester, Guid tenantId);
    Task<Result<VendorRequestDto>> GetActiveRequestForVendorAsync(Guid vendorId, Guid tenantId);
    Task<Result<bool>> ProcessWorkflowActionAsync(Guid requestId, string action, string actor, string? comments, Guid tenantId);
}


