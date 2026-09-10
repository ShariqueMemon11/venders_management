using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

public class VendorContractDtoValidator : AbstractValidator<VendorContractDto>
{
    public VendorContractDtoValidator()
    {
        RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("Contract number is required.")
            .MaximumLength(50);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Contract title is required.")
            .MaximumLength(200);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.ContractValue)
            .GreaterThanOrEqualTo(0).WithMessage("Contract value cannot be negative.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be a 3-letter ISO code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency code must be letters only.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Contract status is not valid.")
            .Must(v => Enum.IsDefined(typeof(ContractStatus), v) && (int)v != 0)
            .WithMessage("Contract status is required.");

        RuleFor(x => x.PaymentTerms)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentTerms));

        RuleFor(x => x.SlaBrief)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.SlaBrief));
    }
}
