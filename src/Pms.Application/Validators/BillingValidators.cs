using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateFolioRequestValidator : AbstractValidator<CreateFolioRequest>
{
    public CreateFolioRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");
    }
}

public class CreateChargeRequestValidator : AbstractValidator<CreateChargeRequest>
{
    public CreateChargeRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 1).WithMessage("VAT rate must be between 0 and 1 (e.g., 0.24 for 24%).");
    }
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than 0.");

        RuleFor(x => x.ProviderReference)
            .MaximumLength(200).When(x => x.ProviderReference is not null)
            .WithMessage("Provider reference cannot exceed 200 characters.");
    }
}
