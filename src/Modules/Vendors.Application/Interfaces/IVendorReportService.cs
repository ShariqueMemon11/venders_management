using Shared.Application.Common.Models;

namespace Vendors.Application.Interfaces;

public interface IVendorReportService
{
    Task<Result<(byte[] FileContents, string ContentType, string FileName)>> GenerateVendorMasterReportAsync(Guid tenantId, string format = "csv");
    Task<Result<(byte[] FileContents, string ContentType, string FileName)>> GenerateRiskReportAsync(Guid tenantId, string format = "csv");
}

