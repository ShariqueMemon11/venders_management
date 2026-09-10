using MediatR;
using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Commands.UpdateVendor;

/// <summary>
/// Profile update only — Status is workflow-owned and must not change from the client DTO.
/// </summary>
public sealed record UpdateVendorCommand(VendorDto Vendor, Guid TenantId)
    : IRequest<Result<bool>>;
