using FluentValidation;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Validators;

public class VendorDtoValidator : AbstractValidator<VendorDto>
{
    public VendorDtoValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(255);

        RuleFor(x => x.TradeName)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.TradeName));

        RuleFor(x => x.TaxRegistrationNumber)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.TaxRegistrationNumber));

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be a 3-letter ISO code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency code must be letters only.");

        RuleFor(x => x.Website)
            .MaximumLength(500)
            .Must(BeValidOptionalUrl).WithMessage("Website must be a valid HTTP or HTTPS URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
    }

    private static bool BeValidOptionalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
