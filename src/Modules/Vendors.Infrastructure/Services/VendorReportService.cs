using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Vendors.Application.Interfaces;
using Shared.Application.Common.Interfaces;
using Platform.Workflow.Interfaces;
using Shared.Application.Common.Models;
using Shared.Infrastructure.Persistence;
using System.Globalization;
using ClosedXML.Excel;
using Vendors.Domain.Enums;

namespace Vendors.Infrastructure.Services;

public class VendorReportService : Vendors.Application.Interfaces.IVendorReportService
{
    private readonly IVendorsDbContext _context;

    public VendorReportService(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<(byte[] FileContents, string ContentType, string FileName)>> GenerateVendorMasterReportAsync(Guid tenantId, string format = "csv")
    {
        var vendors = await _context.Vendors
            .Where(v => v.TenantId == tenantId)
            .Select(v => new
            {
                v.VendorNumber,
                v.LegalName,
                v.TradeName,
                Status = v.Status.ToString(),
                v.TaxRegistrationNumber,
                v.CurrencyCode,
                v.CreatedAt
            })
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            await csvWriter.WriteRecordsAsync(vendors);
            await streamWriter.FlushAsync();
            
            return Result<(byte[], string, string)>.SuccessResult((memoryStream.ToArray(), "text/csv", $"Vendor_Master_Report_{DateTime.UtcNow:yyyyMMdd}.csv"));
        }
        else if (format.ToLower() == "xlsx")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Vendors");

            worksheet.Cell(1, 1).Value = "Vendor Number";
            worksheet.Cell(1, 2).Value = "Legal Name";
            worksheet.Cell(1, 3).Value = "Trade Name";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Tax Registration";
            worksheet.Cell(1, 6).Value = "Currency";
            worksheet.Cell(1, 7).Value = "Created At";
            
            worksheet.Range("A1:G1").Style.Font.Bold = true;
            worksheet.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var vendor in vendors)
            {
                worksheet.Cell(row, 1).Value = vendor.VendorNumber;
                worksheet.Cell(row, 2).Value = vendor.LegalName;
                worksheet.Cell(row, 3).Value = vendor.TradeName;
                worksheet.Cell(row, 4).Value = vendor.Status;
                worksheet.Cell(row, 5).Value = vendor.TaxRegistrationNumber;
                worksheet.Cell(row, 6).Value = vendor.CurrencyCode;
                worksheet.Cell(row, 7).Value = vendor.CreatedAt.ToShortDateString();
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return Result<(byte[], string, string)>.SuccessResult((stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vendor_Master_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx"));
        }

        return Result<(byte[], string, string)>.FailureResult("Unsupported format.");
    }

    public async Task<Result<(byte[] FileContents, string ContentType, string FileName)>> GenerateRiskReportAsync(Guid tenantId, string format = "csv")
    {
        var risks = await _context.VendorRisks
            .Include(r => r.Vendor)
            .Where(r => r.TenantId == tenantId)
            .Select(r => new
            {
                VendorName = r.Vendor.LegalName,
                r.RiskCategory,
                RiskLevel = r.RiskLevel.ToString(),
                r.RiskDescription,
                r.MitigationPlan,
                r.LastReviewDate
            })
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            await csvWriter.WriteRecordsAsync(risks);
            await streamWriter.FlushAsync();
            
            return Result<(byte[], string, string)>.SuccessResult((memoryStream.ToArray(), "text/csv", $"Vendor_Risk_Report_{DateTime.UtcNow:yyyyMMdd}.csv"));
        }
        else if (format.ToLower() == "xlsx")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Risk Assessments");

            worksheet.Cell(1, 1).Value = "Vendor Name";
            worksheet.Cell(1, 2).Value = "Category";
            worksheet.Cell(1, 3).Value = "Risk Level";
            worksheet.Cell(1, 4).Value = "Description";
            worksheet.Cell(1, 5).Value = "Mitigation Plan";
            worksheet.Cell(1, 6).Value = "Last Review";
            
            worksheet.Range("A1:F1").Style.Font.Bold = true;
            worksheet.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.Salmon;

            int row = 2;
            foreach (var risk in risks)
            {
                worksheet.Cell(row, 1).Value = risk.VendorName;
                worksheet.Cell(row, 2).Value = risk.RiskCategory;
                worksheet.Cell(row, 3).Value = risk.RiskLevel;
                worksheet.Cell(row, 4).Value = risk.RiskDescription;
                worksheet.Cell(row, 5).Value = risk.MitigationPlan;
                worksheet.Cell(row, 6).Value = risk.LastReviewDate.HasValue ? risk.LastReviewDate.Value.ToShortDateString() : "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return Result<(byte[], string, string)>.SuccessResult((stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vendor_Risk_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx"));
        }

        return Result<(byte[], string, string)>.FailureResult("Unsupported format.");
    }
}

