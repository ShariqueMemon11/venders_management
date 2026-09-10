using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Models;
using Vendors.Application.Interfaces;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Queries.GetVendorById;

/// <summary>
/// Same projection as the former VendorService.GetByIdAsync.
/// </summary>
public sealed class GetVendorByIdQueryHandler
    : IRequestHandler<GetVendorByIdQuery, Result<VendorDto>>
{
    private readonly IVendorsDbContext _context;

    public GetVendorByIdQueryHandler(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VendorDto>> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        var vendor = await _context.Vendors
            .Where(v => v.Id == request.VendorId && v.TenantId == request.TenantId)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (vendor == null)
            return Result<VendorDto>.FailureResult("Vendor not found.");

        return Result<VendorDto>.SuccessResult(vendor);
    }
}
