using MediatR;
using Shared.Application.Common.Models;
using Vendors.Application.Interfaces;
using Vendors.Domain.Entities;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Commands.CreateVendor;

/// <summary>
/// Same rules as the former VendorService.CreateAsync (Batch 1d):
/// always persist Status = Draft; generate VendorNumber when missing.
/// </summary>
public sealed class CreateVendorCommandHandler
    : IRequestHandler<CreateVendorCommand, Result<Guid>>
{
    private readonly IVendorsDbContext _context;

    public CreateVendorCommandHandler(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var vendorDto = request.Vendor;

        var vendor = new Vendor
        {
            TenantId = request.TenantId,
            VendorNumber = string.IsNullOrEmpty(vendorDto.VendorNumber)
                ? $"V-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}"
                : vendorDto.VendorNumber,
            LegalName = vendorDto.LegalName,
            TradeName = vendorDto.TradeName,
            // Draft-then-submit: create never starts workflow; client cannot escalate status here.
            Status = VendorStatus.Draft,
            TaxRegistrationNumber = vendorDto.TaxRegistrationNumber,
            Website = vendorDto.Website,
            CurrencyCode = vendorDto.CurrencyCode
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.SuccessResult(vendor.Id);
    }
}
