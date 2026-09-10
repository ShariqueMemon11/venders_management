using MediatR;
using Shared.Application.Common.Models;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Commands.CreateVendor;

/// <summary>
/// Creates a vendor Draft only — never starts workflow; ignores client Status.
/// FluentValidation still runs on <see cref="VendorDto"/> at the API boundary
/// via AddFluentValidationAutoValidation (Batch 3a).
/// </summary>
public sealed record CreateVendorCommand(VendorDto Vendor, Guid TenantId)
    : IRequest<Result<Guid>>;
