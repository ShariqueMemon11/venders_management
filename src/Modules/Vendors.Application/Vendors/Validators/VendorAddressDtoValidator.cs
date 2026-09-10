using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

public class VendorAddressDtoValidator : AbstractValidator<VendorAddressDto>
{
    public VendorAddressDtoValidator()
    {
        RuleFor(x => x.AddressType)
            .IsInEnum().WithMessage("Address type is not valid.")
            .Must(v => Enum.IsDefined(typeof(AddressType), v) && (int)v != 0)
            .WithMessage("Address type is required.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(255);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressLine2));

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.StateProvince)
            .NotEmpty().WithMessage("State / province is required.")
            .MaximumLength(100);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
    }
}
