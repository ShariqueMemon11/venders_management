using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Models;
using Vendors.Application.Interfaces;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Queries.GetVendorDirectory;

/// <summary>
/// Same paging/filter projection as the former VendorService.GetPagedVendorsAsync.
/// </summary>
public sealed class GetVendorDirectoryQueryHandler
    : IRequestHandler<GetVendorDirectoryQuery, Result<PagedResult<VendorDirectoryDto>>>
{
    private readonly IVendorsDbContext _context;

    public GetVendorDirectoryQueryHandler(IVendorsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<VendorDirectoryDto>>> Handle(
        GetVendorDirectoryQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Vendors
            .Where(v => v.TenantId == request.TenantId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim().ToLower();
            query = query.Where(v =>
                v.LegalName.ToLower().Contains(term) ||
                v.TradeName.ToLower().Contains(term) ||
                v.VendorNumber.ToLower().Contains(term));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(v => v.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var countryTerm = request.Country.Trim().ToLower();
            query = query.Where(v =>
                v.Addresses.Any(a => a.Country.ToLower().Contains(countryTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var vendors = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorDirectoryDto
            {
                Id = v.Id,
                VendorNumber = v.VendorNumber,
                LegalName = v.LegalName,
                TradeName = v.TradeName,
                TaxRegistrationNumber = v.TaxRegistrationNumber,
                Status = v.Status,
                CreatedAt = v.CreatedAt,
                PrimaryContactName = v.Contacts.Where(c => c.IsPrimary).Select(c => c.Name).FirstOrDefault(),
                PrimaryContactEmail = v.Contacts.Where(c => c.IsPrimary).Select(c => c.Email).FirstOrDefault(),
                PrimaryContactPhone = v.Contacts.Where(c => c.IsPrimary).Select(c => c.Phone).FirstOrDefault(),
                PrimaryAddressCity = v.Addresses.Where(a => a.AddressType == AddressType.HeadOffice).Select(a => a.City).FirstOrDefault(),
                PrimaryAddressCountry = v.Addresses.Where(a => a.AddressType == AddressType.HeadOffice).Select(a => a.Country).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var result = new PagedResult<VendorDirectoryDto>
        {
            Items = vendors,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return Result<PagedResult<VendorDirectoryDto>>.SuccessResult(result);
    }
}
