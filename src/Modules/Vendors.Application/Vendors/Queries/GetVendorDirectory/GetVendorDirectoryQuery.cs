using MediatR;
using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Queries.GetVendorDirectory;

public sealed record GetVendorDirectoryQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 10,
    string? Name = null,
    VendorStatus? Status = null,
    string? Country = null)
    : IRequest<Result<PagedResult<VendorDirectoryDto>>>;
