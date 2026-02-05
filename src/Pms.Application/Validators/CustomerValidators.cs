using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).When(x => x.Phone is not null)
            .WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => x.Address is not null)
            .WithMessage("Address cannot exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).When(x => x.Name is not null)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).When(x => x.Phone is not null)
            .WithMessage("Phone cannot exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}
