using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeeSheet.Domain;

namespace TeeSheet.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.BookingId);

        // VARCHAR(10) holding the enum name
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();

        builder.Property(b => b.CreatedUtc).HasColumnType("datetime2(0)");
        builder.Property(b => b.HoldExpiresUtc).HasColumnType("datetime2(0)");
        builder.Property(b => b.IdempotencyKey).HasMaxLength(64).IsUnicode(false);

        builder.HasOne(b => b.TeeTime)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TeeTimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Golfer)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GolferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
