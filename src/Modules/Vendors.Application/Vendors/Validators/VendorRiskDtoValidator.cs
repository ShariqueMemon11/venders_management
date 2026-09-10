using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

public class VendorRiskDtoValidator : AbstractValidator<VendorRiskDto>
{
    public VendorRiskDtoValidator()
    {
        RuleFor(x => x.RiskCategory)
            .NotEmpty().WithMessage("Risk category is required.")
            .MaximumLength(200);

        RuleFor(x => x.RiskLevel)
            .IsInEnum().WithMessage("Risk level is not valid.")
            .Must(v => Enum.IsDefined(typeof(RiskLevel), v) && (int)v != 0)
            .WithMessage("Risk level is required.");

        RuleFor(x => x.RiskDescription)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.RiskDescription));

        RuleFor(x => x.MitigationPlan)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.MitigationPlan));
    }
}
