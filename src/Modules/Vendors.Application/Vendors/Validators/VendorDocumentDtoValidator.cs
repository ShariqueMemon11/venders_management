using FluentValidation;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Validators;

/// <summary>
/// Validates document metadata DTOs. Multipart upload still uses form fields;
/// this covers any JSON create/update that binds <see cref="VendorDocumentDto"/>.
/// </summary>
public class VendorDocumentDtoValidator : AbstractValidator<VendorDocumentDto>
{
    public VendorDocumentDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Document title is required.")
            .MaximumLength(200);

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Document type is not valid.")
            .Must(v => Enum.IsDefined(typeof(DocumentType), v) && (int)v != 0)
            .WithMessage("Document type is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Document status is not valid.")
            .When(x => (int)x.Status != 0);

        RuleFor(x => x.VerificationComments)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.VerificationComments));
    }
}
