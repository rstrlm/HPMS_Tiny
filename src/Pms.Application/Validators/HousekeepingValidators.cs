using FluentValidation;
using Pms.Application.DTOs;

namespace Pms.Application.Validators;

public class CreateCleaningTaskRequestValidator : AbstractValidator<CreateCleaningTaskRequest>
{
    public CreateCleaningTaskRequestValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class UpdateCleaningTaskRequestValidator : AbstractValidator<UpdateCleaningTaskRequest>
{
    public UpdateCleaningTaskRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
