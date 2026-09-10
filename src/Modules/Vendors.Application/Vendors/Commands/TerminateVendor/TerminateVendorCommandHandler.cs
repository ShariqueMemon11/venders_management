using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Models;
using Vendors.Application.Interfaces;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Commands.TerminateVendor;

/// <summary>
/// Same rules as the former VendorService.TerminateAsync:
/// sets Status = Terminated; keeps the row (IsDeleted stays false).
/// </summary>
public sealed class TerminateVendorCommandHandler
    : IRequestHandler<TerminateVendorCommand, Result<bool>>
{
    private readonly IVendorsDbContext _context;

    public TerminateVendorCommandHandler(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(TerminateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(
                v => v.Id == request.VendorId && v.TenantId == request.TenantId,
                cancellationToken);

        if (vendor == null)
            return Result<bool>.FailureResult("Vendor not found.");

        // Enterprise pattern: terminate keeps the record for audit/history (no hard/soft-delete).
        vendor.Status = VendorStatus.Terminated;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.SuccessResult(true);
    }
}
