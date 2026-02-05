using Microsoft.EntityFrameworkCore;
using Pms.Domain.Common;
using Pms.Domain.Entities;

namespace Pms.Infrastructure.Persistence;

public class PmsDbContext : DbContext
{
    public PmsDbContext(DbContextOptions<PmsDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomStateBlock> RoomStateBlocks => Set<RoomStateBlock>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<RoomAssignment> RoomAssignments => Set<RoomAssignment>();
    public DbSet<ReservationHold> ReservationHolds => Set<ReservationHold>();
    public DbSet<TreatmentType> TreatmentTypes => Set<TreatmentType>();
    public DbSet<TreatmentRoom> TreatmentRooms => Set<TreatmentRoom>();
    public DbSet<TreatmentAppointment> TreatmentAppointments => Set<TreatmentAppointment>();
    public DbSet<CleaningTask> CleaningTasks => Set<CleaningTask>();
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<BrandingSetting> BrandingSettings => Set<BrandingSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PmsDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    if (entry.Entity.Id == Guid.Empty)
                        entry.Entity.Id = Guid.NewGuid();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
