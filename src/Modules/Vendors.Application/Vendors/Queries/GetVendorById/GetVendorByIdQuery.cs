using MediatR;
using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Queries.GetVendorById;

public sealed record GetVendorByIdQuery(Guid VendorId, Guid TenantId)
    : IRequest<Result<VendorDto>>;
