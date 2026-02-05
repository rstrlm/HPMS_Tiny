using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        // Either CustomerId or NewCustomer must be provided
        RuleFor(x => x)
            .Must(x => x.CustomerId.HasValue || x.NewCustomer is not null)
            .WithMessage("Either Customer ID or New Customer details must be provided.");

        // If CustomerId is provided, it must not be empty
        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty).When(x => x.CustomerId.HasValue)
            .WithMessage("Customer ID cannot be empty.");

        // If NewCustomer is provided, validate it
        RuleFor(x => x.NewCustomer!.Name)
            .NotEmpty().When(x => x.NewCustomer is not null)
            .WithMessage("Customer name is required.");

        RuleFor(x => x.CheckInDate)
            .NotEmpty().WithMessage("Check-in date is required.");

        RuleFor(x => x.CheckOutDate)
            .NotEmpty().WithMessage("Check-out date is required.")
            .GreaterThan(x => x.CheckInDate).WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x.NumberOfGuests)
            .GreaterThan(0).WithMessage("Number of guests must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("Number of guests cannot exceed 50.");

        RuleFor(x => x.RoomAssignments)
            .NotEmpty().WithMessage("At least one room assignment is required.");

        RuleForEach(x => x.RoomAssignments)
            .SetValidator(new CreateRoomAssignmentRequestValidator());
    }
}

public class CreateRoomAssignmentRequestValidator : AbstractValidator<CreateRoomAssignmentRequest>
{
    public CreateRoomAssignmentRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("From date is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("To date is required.")
            .GreaterThan(x => x.FromDate).WithMessage("To date must be after from date.");
    }
}

public class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationRequestValidator()
    {
        RuleFor(x => x.NumberOfGuests)
            .GreaterThan(0).When(x => x.NumberOfGuests.HasValue)
            .WithMessage("Number of guests must be at least 1.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}
