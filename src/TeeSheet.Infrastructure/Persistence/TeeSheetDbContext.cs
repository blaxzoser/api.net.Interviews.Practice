using Microsoft.EntityFrameworkCore;
using TeeSheet.Domain;

namespace TeeSheet.Infrastructure.Persistence;

/// <summary>
/// Maps onto the EXISTING schema created by sql/01-schema.sql. There are no EF migrations on purpose
/// (migrations are practised separately in exercise B5).
/// </summary>
public class TeeSheetDbContext(DbContextOptions<TeeSheetDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<TeeTime> TeeTimes => Set<TeeTime>();
    public DbSet<Golfer> Golfers => Set<Golfer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeeSheetDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQL Server returns DateTimeKind.Unspecified. Everything we store is UTC, so say so on the way out.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }
}
