using FluentValidation;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Validators;

public class VendorPerformanceDtoValidator : AbstractValidator<VendorPerformanceDto>
{
    public VendorPerformanceDtoValidator()
    {
        RuleFor(x => x.EvaluationPeriod)
            .NotEmpty().WithMessage("Evaluation period is required.")
            .MaximumLength(100);

        RuleFor(x => x.QualityScore)
            .InclusiveBetween(0, 100).WithMessage("Quality score must be between 0 and 100.");

        RuleFor(x => x.DeliveryScore)
            .InclusiveBetween(0, 100).WithMessage("Delivery score must be between 0 and 100.");

        RuleFor(x => x.ResponsivenessScore)
            .InclusiveBetween(0, 100).WithMessage("Responsiveness score must be between 0 and 100.");

        RuleFor(x => x.ComplianceScore)
            .InclusiveBetween(0, 100).WithMessage("Compliance score must be between 0 and 100.");

        RuleFor(x => x.Comments)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Comments));

        RuleFor(x => x.Evaluator)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Evaluator));
    }
}
