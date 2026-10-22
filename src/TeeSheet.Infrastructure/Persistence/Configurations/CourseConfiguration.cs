using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeeSheet.Domain;

namespace TeeSheet.Infrastructure.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.CourseId);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();                       // NVARCHAR(100)
        builder.Property(c => c.TimeZoneId).HasMaxLength(50).IsUnicode(false).IsRequired(); // VARCHAR(50)
        builder.Property(c => c.Country).HasMaxLength(2).IsUnicode(false).IsFixedLength().IsRequired();  // CHAR(2)
        builder.Property(c => c.Currency).HasMaxLength(3).IsUnicode(false).IsFixedLength().IsRequired(); // CHAR(3)
    }
}
