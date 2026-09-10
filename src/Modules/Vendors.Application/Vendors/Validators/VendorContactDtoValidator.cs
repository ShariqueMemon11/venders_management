using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

public class VendorContactDtoValidator : AbstractValidator<VendorContactDto>
{
    public VendorContactDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.")
            .MaximumLength(256);

        RuleFor(x => x.JobTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.JobTitle));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Mobile)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile));

        RuleFor(x => x.ContactType)
            .IsInEnum().WithMessage("Contact type is not valid.")
            .Must(v => Enum.IsDefined(typeof(ContactType), v) && (int)v != 0)
            .WithMessage("Contact type is required.");
    }
}
