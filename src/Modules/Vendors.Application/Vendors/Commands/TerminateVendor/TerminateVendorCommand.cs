using MediatR;
using Shared.Application.Common.Models;

namespace Vendors.Application.Vendors.Commands.TerminateVendor;

/// <summary>
/// Soft-terminates a vendor (Status = Terminated). Does not set IsDeleted.
/// </summary>
public sealed record TerminateVendorCommand(Guid VendorId, Guid TenantId)
    : IRequest<Result<bool>>;
