using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeeSheet.Domain;

namespace TeeSheet.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.PaymentId);

        builder.Property(p => p.Amount).HasPrecision(8, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired(); // CHAR(3)
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(p => p.CreatedUtc).HasColumnType("datetime2(0)");

        builder.HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
