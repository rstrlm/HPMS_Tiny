using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class UpdateBrandingRequestValidator : AbstractValidator<UpdateBrandingRequest>
{
    public UpdateBrandingRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.");

        RuleFor(x => x.CompanyLegalName)
            .NotEmpty().WithMessage("Company legal name is required.")
            .MaximumLength(200).WithMessage("Company legal name cannot exceed 200 characters.");

        RuleFor(x => x.Tagline)
            .MaximumLength(500).WithMessage("Tagline cannot exceed 500 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithMessage("Tax ID cannot exceed 50 characters.");

        RuleFor(x => x.BankName)
            .MaximumLength(200).WithMessage("Bank name cannot exceed 200 characters.");

        RuleFor(x => x.IBAN)
            .MaximumLength(50).WithMessage("IBAN cannot exceed 50 characters.");

        RuleFor(x => x.BIC)
            .MaximumLength(20).WithMessage("BIC cannot exceed 20 characters.");
    }
}
