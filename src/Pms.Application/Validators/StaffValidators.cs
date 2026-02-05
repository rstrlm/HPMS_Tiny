using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateStaffProfileRequestValidator : AbstractValidator<CreateStaffProfileRequest>
{
    public CreateStaffProfileRequestValidator()
    {
        RuleFor(x => x.KeycloakUserId)
            .NotEmpty().WithMessage("Keycloak user ID is required.")
            .MaximumLength(100).WithMessage("Keycloak user ID cannot exceed 100 characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Skills)
            .MaximumLength(500).When(x => x.Skills is not null)
            .WithMessage("Skills cannot exceed 500 characters.");
    }
}

public class UpdateStaffProfileRequestValidator : AbstractValidator<UpdateStaffProfileRequest>
{
    public UpdateStaffProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(200).When(x => x.DisplayName is not null)
            .WithMessage("Display name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format.");

        RuleFor(x => x.Skills)
            .MaximumLength(500).When(x => x.Skills is not null)
            .WithMessage("Skills cannot exceed 500 characters.");
    }
}
