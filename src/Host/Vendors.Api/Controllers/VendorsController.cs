using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Vendors.Application.Interfaces;
using Shared.Application.Common.Interfaces;
using Vendors.Application.Vendors.Commands.CreateVendor;
using Vendors.Application.Vendors.Commands.TerminateVendor;
using Vendors.Application.Vendors.Commands.UpdateVendor;
using Vendors.Application.Vendors.Dtos;
using Vendors.Application.Vendors.Queries.GetVendorById;
using Vendors.Application.Vendors.Queries.GetVendorDirectory;
using Vendors.Domain.Enums;

namespace Vendors.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Require authentication for all endpoints by default
public class VendorsController : ControllerBase
{
    private readonly IVendorService _vendorService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVendorReportService _vendorReportService;
    private readonly IMediator _mediator;

    public VendorsController(
        IVendorService vendorService,
        ICurrentUserService currentUserService,
        IVendorReportService vendorReportService,
        IMediator mediator)
    {
        _vendorService = vendorService;
        _currentUserService = currentUserService;
        _vendorReportService = vendorReportService;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _vendorService.GetAllAsync(_currentUserService.TenantId);
        return Ok(result);
    }

    [HttpGet("directory")]
    public async Task<IActionResult> GetDirectory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] VendorStatus? status = null,
        [FromQuery] string? country = null)
    {
        var result = await _mediator.Send(new GetVendorDirectoryQuery(
            _currentUserService.TenantId, page, pageSize, name, status, country));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(
            new GetVendorByIdQuery(id, _currentUserService.TenantId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetDetailsById(Guid id)
    {
        var result = await _vendorService.GetDetailsByIdAsync(id, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Perm.Vendor.Create")]
    public async Task<IActionResult> Create(VendorDto vendorDto)
    {
        // VendorDto still validated by FluentValidation auto-validation (3a) before this runs.
        var result = await _mediator.Send(
            new CreateVendorCommand(vendorDto, _currentUserService.TenantId));
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> Update(Guid id, VendorDto vendorDto)
    {
        if (id != vendorDto.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(
            new UpdateVendorCommand(vendorDto, _currentUserService.TenantId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id}/terminate")]
    [Authorize(Policy = "Perm.Vendor.Terminate")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var result = await _mediator.Send(
            new TerminateVendorCommand(id, _currentUserService.TenantId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Perm.Vendor.Terminate")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Kept for backward compatibility — soft-terminates, does not hard delete.
        var result = await _mediator.Send(
            new TerminateVendorCommand(id, _currentUserService.TenantId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Contacts — Vendor.Edit (procurement child-record writes)
    [HttpPost("{id}/contacts")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> AddContact(Guid id, VendorContactDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddContactAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("contacts/{contactId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> UpdateContact(Guid contactId, VendorContactDto dto)
    {
        dto.Id = contactId;
        var result = await _vendorService.UpdateContactAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("contacts/{contactId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> DeleteContact(Guid contactId)
    {
        var result = await _vendorService.DeleteContactAsync(contactId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Addresses
    [HttpPost("{id}/addresses")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> AddAddress(Guid id, VendorAddressDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddAddressAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("addresses/{addressId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> UpdateAddress(Guid addressId, VendorAddressDto dto)
    {
        dto.Id = addressId;
        var result = await _vendorService.UpdateAddressAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("addresses/{addressId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        var result = await _vendorService.DeleteAddressAsync(addressId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Bank Accounts
    [HttpPost("{id}/bankaccounts")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> AddBankAccount(Guid id, BankAccountDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddBankAccountAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("bankaccounts/{bankAccountId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> UpdateBankAccount(Guid bankAccountId, BankAccountDto dto)
    {
        dto.Id = bankAccountId;
        var result = await _vendorService.UpdateBankAccountAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("bankaccounts/{bankAccountId}")]
    [Authorize(Policy = "Perm.Vendor.Edit")]
    public async Task<IActionResult> DeleteBankAccount(Guid bankAccountId)
    {
        var result = await _vendorService.DeleteBankAccountAsync(bankAccountId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Documents
    [HttpPost("{id}/documents/upload")]
    [Authorize(Policy = "Perm.Document.Upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] string title, [FromForm] DocumentType type, [FromForm] DateTime? expiryDate, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var stream = file.OpenReadStream();
        var result = await _vendorService.UploadDocumentAsync(id, title, type, expiryDate, stream, file.FileName, file.ContentType, _currentUserService.TenantId);
        
        if (!result.Success) return BadRequest(result.Message);
        
        return Ok(result);
    }

    [HttpGet("documents/{documentId}/download")]
    [Authorize(Policy = "Perm.Document.Download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId)
    {
        var result = await _vendorService.DownloadDocumentAsync(documentId, _currentUserService.TenantId);
        if (!result.Success || result.Data.FileStream == null)
            return NotFound(result.Message);

        return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPatch("documents/{documentId}/status")]
    [Authorize(Policy = "Perm.Document.Verify")]
    public async Task<IActionResult> UpdateDocumentStatus(Guid documentId, [FromQuery] DocumentStatus status, [FromQuery] string? comments)
    {
        var result = await _vendorService.UpdateDocumentStatusAsync(documentId, status, comments, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("documents/{documentId}")]
    [Authorize(Policy = "Perm.Document.Delete")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
    {
        var result = await _vendorService.DeleteDocumentAsync(documentId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Contracts
    [HttpPost("{id}/contracts")]
    [Authorize(Policy = "Perm.Contract.Edit")]
    public async Task<IActionResult> AddContract(Guid id, VendorContractDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddContractAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("contracts/{contractId}")]
    [Authorize(Policy = "Perm.Contract.Edit")]
    public async Task<IActionResult> UpdateContract(Guid contractId, VendorContractDto dto)
    {
        dto.Id = contractId;
        var result = await _vendorService.UpdateContractAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("contracts/{contractId}")]
    [Authorize(Policy = "Perm.Contract.Edit")]
    public async Task<IActionResult> DeleteContract(Guid contractId)
    {
        var result = await _vendorService.DeleteContractAsync(contractId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Compliance
    [HttpPost("{id}/compliance")]
    [Authorize(Policy = "Perm.Compliance.Edit")]
    public async Task<IActionResult> AddCompliance(Guid id, VendorComplianceDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddComplianceAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("compliance/{complianceId}")]
    [Authorize(Policy = "Perm.Compliance.Edit")]
    public async Task<IActionResult> UpdateCompliance(Guid complianceId, VendorComplianceDto dto)
    {
        dto.Id = complianceId;
        var result = await _vendorService.UpdateComplianceAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("compliance/{complianceId}")]
    [Authorize(Policy = "Perm.Compliance.Edit")]
    public async Task<IActionResult> DeleteCompliance(Guid complianceId)
    {
        var result = await _vendorService.DeleteComplianceAsync(complianceId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Performance
    [HttpPost("{id}/performances")]
    [Authorize(Policy = "Perm.Performance.Edit")]
    public async Task<IActionResult> AddPerformance(Guid id, VendorPerformanceDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddPerformanceAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("performances/{performanceId}")]
    [Authorize(Policy = "Perm.Performance.Edit")]
    public async Task<IActionResult> UpdatePerformance(Guid performanceId, VendorPerformanceDto dto)
    {
        dto.Id = performanceId;
        var result = await _vendorService.UpdatePerformanceAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("performances/{performanceId}")]
    [Authorize(Policy = "Perm.Performance.Edit")]
    public async Task<IActionResult> DeletePerformance(Guid performanceId)
    {
        var result = await _vendorService.DeletePerformanceAsync(performanceId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Risk
    [HttpPost("{id}/risks")]
    [Authorize(Policy = "Perm.Risk.Edit")]
    public async Task<IActionResult> AddRisk(Guid id, VendorRiskDto dto)
    {
        dto.VendorId = id;
        var result = await _vendorService.AddRiskAsync(dto, _currentUserService.TenantId);
        return Ok(result);
    }

    [HttpPut("risks/{riskId}")]
    [Authorize(Policy = "Perm.Risk.Edit")]
    public async Task<IActionResult> UpdateRisk(Guid riskId, VendorRiskDto dto)
    {
        dto.Id = riskId;
        var result = await _vendorService.UpdateRiskAsync(dto, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("risks/{riskId}")]
    [Authorize(Policy = "Perm.Risk.Edit")]
    public async Task<IActionResult> DeleteRisk(Guid riskId)
    {
        var result = await _vendorService.DeleteRiskAsync(riskId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // Dashboard & Approval Endpoints
    [HttpGet("reports/master")]
    [Authorize(Policy = "Perm.Report.VendorMaster")]
    public async Task<IActionResult> DownloadMasterReport([FromQuery] string format = "csv")
    {
        var result = await _vendorReportService.GenerateVendorMasterReportAsync(_currentUserService.TenantId, format);
        if (!result.Success) return BadRequest(result.Message);

        return File(result.Data.FileContents, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("reports/risk")]
    [Authorize(Policy = "Perm.Report.Risk")]
    public async Task<IActionResult> DownloadRiskReport([FromQuery] string format = "csv")
    {
        var result = await _vendorReportService.GenerateRiskReportAsync(_currentUserService.TenantId, format);
        if (!result.Success) return BadRequest(result.Message);

        return File(result.Data.FileContents, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var result = await _vendorService.GetDashboardSummaryAsync(_currentUserService.TenantId);
        return Ok(result);
    }

    // Workflow Endpoints
    [HttpGet("requests/pending")]
    [Authorize(Policy = "Perm.Workflow.View")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await _vendorService.GetPendingRequestsAsync(_currentUserService.TenantId);
        return Ok(result);
    }

    [HttpGet("requests/{requestId}")]
    [Authorize(Policy = "Perm.Workflow.View")]
    public async Task<IActionResult> GetRequestDetails(Guid requestId)
    {
        var result = await _vendorService.GetRequestDetailsAsync(requestId, _currentUserService.TenantId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Removed: instant onboard bypassed Draft. Use POST /vendors then submit-for-approval.
    /// </summary>
    [HttpPost("requests/onboard")]
    [Authorize(Policy = "Perm.Vendor.Create")]
    public IActionResult SubmitRegistrationRequestRemoved()
    {
        return StatusCode(StatusCodes.Status410Gone, new
        {
            message = "This endpoint was removed. Create a Draft with POST /vendors, add details, then POST /vendors/{id}/submit-for-approval."
        });
    }

    [HttpPost("{id}/submit-for-approval")]
    [Authorize(Policy = "Perm.Vendor.Submit")]
    public async Task<IActionResult> SubmitForApproval(Guid id)
    {
        var result = await _vendorService.SubmitVendorForApprovalAsync(id, _currentUserService.UserId ?? "Unknown User", _currentUserService.TenantId);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { success = true, message = "Submitted successfully" });
    }

    [HttpGet("{id}/active-request")]
    public async Task<IActionResult> GetActiveRequest(Guid id)
    {
        var result = await _vendorService.GetActiveRequestForVendorAsync(id, _currentUserService.TenantId);
        if (!result.Success) return NotFound(new { message = result.Message }); // Return 404 if no active request
        return Ok(result);
    }

    [HttpPost("requests/{requestId}/workflow")]
    [Authorize(Policy = "Perm.Workflow.Approve")]
    public async Task<IActionResult> ProcessWorkflowAction(Guid requestId, [FromQuery] string action, [FromBody] string? comments)
    {
        var result = await _vendorService.ProcessWorkflowActionAsync(requestId, action, _currentUserService.UserId ?? "Unknown User", comments, _currentUserService.TenantId);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(result);
    }
}


