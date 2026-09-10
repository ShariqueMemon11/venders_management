using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

public class VendorComplianceDtoValidator : AbstractValidator<VendorComplianceDto>
{
    public VendorComplianceDtoValidator()
    {
        RuleFor(x => x.RequirementName)
            .NotEmpty().WithMessage("Requirement name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Compliance status is not valid.")
            .Must(v => Enum.IsDefined(typeof(ComplianceStatus), v) && (int)v != 0)
            .WithMessage("Compliance status is required.");

        RuleFor(x => x.CheckedBy)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.CheckedBy));

        RuleFor(x => x.Comments)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comments));
    }
}
