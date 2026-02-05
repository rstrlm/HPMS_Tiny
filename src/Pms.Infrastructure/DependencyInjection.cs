using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pms.Application.Interfaces;
using Pms.Application.Settings;
using Pms.Infrastructure.Keycloak;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PmsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("PmsDb"),
                b => b.MigrationsAssembly(typeof(PmsDbContext).Assembly.FullName)));

        // Configure branding settings (used as seed defaults for DB-backed settings)
        services.Configure<BrandingSettings>(configuration.GetSection("Branding"));
        services.AddMemoryCache();
        services.AddScoped<IBrandingService, BrandingService>();

        // Configure Keycloak Admin service (optional - only if settings are provided)
        var keycloakAdminSection = configuration.GetSection("KeycloakAdmin");
        if (keycloakAdminSection.Exists())
        {
            services.Configure<KeycloakAdminSettings>(keycloakAdminSection);
            services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>();
        }

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IRoomTypeService, RoomTypeService>();
        services.AddScoped<IRoomStateBlockService, RoomStateBlockService>();
        services.AddScoped<IRoomAvailabilityService, RoomAvailabilityService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ITreatmentAvailabilityService, TreatmentAvailabilityService>();
        services.AddScoped<ITreatmentTypeService, TreatmentTypeService>();
        services.AddScoped<ITreatmentRoomService, TreatmentRoomService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IHousekeepingService, HousekeepingService>();
        services.AddScoped<IFolioService, FolioService>();
        services.AddScoped<IStaffService, StaffService>();

        return services;
    }
}
