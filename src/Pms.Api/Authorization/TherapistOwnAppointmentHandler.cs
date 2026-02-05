using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pms.Infrastructure.Persistence;
using System.Security.Claims;

namespace Pms.Api.Authorization;

public class TherapistOwnAppointmentHandler : AuthorizationHandler<TherapistOwnAppointmentRequirement, Guid>
{
    private readonly IServiceProvider _serviceProvider;

    public TherapistOwnAppointmentHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TherapistOwnAppointmentRequirement requirement,
        Guid appointmentId)
    {
        var user = context.User;

        // Managers can access any appointment
        if (user.IsInRole("manager"))
        {
            context.Succeed(requirement);
            return;
        }

        // Frontdesk can access any appointment
        if (user.IsInRole("frontdesk"))
        {
            context.Succeed(requirement);
            return;
        }

        // Therapists can only access their own appointments
        if (user.IsInRole("therapist"))
        {
            var keycloakUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                context.Fail();
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PmsDbContext>();

            // Find staff profile for this user
            var staffProfile = await dbContext.StaffProfiles
                .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId);

            if (staffProfile is null)
            {
                context.Fail();
                return;
            }

            // Check if appointment is assigned to this therapist
            var appointment = await dbContext.TreatmentAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment is not null && appointment.TherapistStaffId == staffProfile.Id)
            {
                context.Succeed(requirement);
                return;
            }
        }

        context.Fail();
    }
}
