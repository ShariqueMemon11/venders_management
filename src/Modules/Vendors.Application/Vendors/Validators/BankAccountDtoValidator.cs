using System.Text.RegularExpressions;
using FluentValidation;
using Vendors.Application.Vendors.Dtos;

namespace Vendors.Application.Vendors.Validators;

public class BankAccountDtoValidator : AbstractValidator<BankAccountDto>
{
    // ISO 13616-ish: 2-letter country + 2 check digits + BBAN (total 15–34 alphanumeric).
    private static readonly Regex IbanRegex = new(
        @"^[A-Za-z]{2}[0-9]{2}[A-Za-z0-9]{11,30}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SwiftRegex = new(
        @"^[A-Za-z]{4}[A-Za-z]{2}[A-Za-z0-9]{2}([A-Za-z0-9]{3})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public BankAccountDtoValidator()
    {
        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(200);

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(200);

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(50);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be a 3-letter ISO code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency code must be letters only.");

        RuleFor(x => x.Iban)
            .Must(iban => string.IsNullOrWhiteSpace(iban) || IbanRegex.IsMatch(iban.Replace(" ", "")))
            .WithMessage("IBAN format is invalid.");

        RuleFor(x => x.SwiftBic)
            .Must(swift => string.IsNullOrWhiteSpace(swift) || SwiftRegex.IsMatch(swift.Replace(" ", "")))
            .WithMessage("SWIFT/BIC format is invalid.");
    }
}
