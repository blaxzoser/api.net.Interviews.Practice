using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeeSheet.Domain;

namespace TeeSheet.Infrastructure.Persistence.Configurations;

internal sealed class GolferConfiguration : IEntityTypeConfiguration<Golfer>
{
    public void Configure(EntityTypeBuilder<Golfer> builder)
    {
        builder.ToTable("Golfers");
        builder.HasKey(g => g.GolferId);

        builder.Property(g => g.FirstName).HasMaxLength(50).IsRequired();                    // NVARCHAR(50)
        builder.Property(g => g.LastName).HasMaxLength(50).IsRequired();                     // NVARCHAR(50)
        builder.Property(g => g.Email).HasMaxLength(200).IsUnicode(false).IsRequired();      // VARCHAR(200)
    }
}
