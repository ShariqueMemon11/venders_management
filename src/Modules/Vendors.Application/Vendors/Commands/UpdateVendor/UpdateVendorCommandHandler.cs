using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Models;
using Vendors.Application.Interfaces;

namespace Vendors.Application.Vendors.Commands.UpdateVendor;

/// <summary>
/// Same rules as the former VendorService.UpdateAsync:
/// updates profile fields; never mutates Status from the client.
/// </summary>
public sealed class UpdateVendorCommandHandler
    : IRequestHandler<UpdateVendorCommand, Result<bool>>
{
    private readonly IVendorsDbContext _context;

    public UpdateVendorCommandHandler(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendorDto = request.Vendor;

        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(
                v => v.Id == vendorDto.Id && v.TenantId == request.TenantId,
                cancellationToken);

        if (vendor == null)
            return Result<bool>.FailureResult("Vendor not found.");

        vendor.LegalName = vendorDto.LegalName;
        vendor.TradeName = vendorDto.TradeName;
        // Status is workflow-owned (create→Draft, submit→PendingReview, approve/terminate endpoints).
        // Do not accept client-supplied status on profile update.
        vendor.TaxRegistrationNumber = vendorDto.TaxRegistrationNumber;
        vendor.Website = vendorDto.Website;
        vendor.CurrencyCode = vendorDto.CurrencyCode;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.SuccessResult(true);
    }
}
