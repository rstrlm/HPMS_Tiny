using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.TreatmentTypeId)
            .NotEmpty().WithMessage("Treatment type ID is required.");

        RuleFor(x => x.TreatmentRoomId)
            .NotEmpty().WithMessage("Treatment room ID is required.");

        RuleFor(x => x.StartAtUtc)
            .NotEmpty().WithMessage("Start time is required.")
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Start time cannot be in the past.");

        RuleFor(x => x.SeatsUsed)
            .GreaterThan(0).WithMessage("Seats used must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Seats used cannot exceed 100.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        RuleFor(x => x.StartAtUtc)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .When(x => x.StartAtUtc.HasValue)
            .WithMessage("Start time cannot be in the past.");

        RuleFor(x => x.SeatsUsed)
            .GreaterThan(0).When(x => x.SeatsUsed.HasValue)
            .WithMessage("Seats used must be at least 1.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
