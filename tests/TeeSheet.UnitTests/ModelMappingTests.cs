using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TeeSheet.Domain;
using TeeSheet.Infrastructure.Persistence;

namespace TeeSheet.UnitTests;

/// <summary>
/// Cheap guard rails: the EF model must keep matching sql/01-schema.sql. No database needed,
/// the model is built but never connected.
/// </summary>
public class ModelMappingTests
{
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<TeeSheetDbContext>()
            .UseSqlServer("Server=(local);Database=NeverConnected;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var db = new TeeSheetDbContext(options);
        return db.GetService<IDesignTimeModel>().Model;
    }

    [Theory]
    [InlineData(typeof(Course), "Courses")]
    [InlineData(typeof(TeeTime), "TeeTimes")]
    [InlineData(typeof(Golfer), "Golfers")]
    [InlineData(typeof(Booking), "Bookings")]
    [InlineData(typeof(Payment), "Payments")]
    public void Entities_map_to_the_existing_tables(Type entity, string table)
    {
        var entityType = BuildModel().FindEntityType(entity);

        Assert.NotNull(entityType);
        Assert.Equal(table, entityType.GetTableName());
    }

    [Fact]
    public void TeeTime_RowVersion_is_the_concurrency_token()
    {
        var property = BuildModel().FindEntityType(typeof(TeeTime))!.FindProperty(nameof(TeeTime.RowVersion));

        Assert.NotNull(property);
        Assert.True(property.IsConcurrencyToken);
    }

    [Fact]
    public void Statuses_are_stored_as_text()
    {
        var model = BuildModel();

        var booking = model.FindEntityType(typeof(Booking))!.FindProperty(nameof(Booking.Status))!;
        var payment = model.FindEntityType(typeof(Payment))!.FindProperty(nameof(Payment.Status))!;

        Assert.Equal(typeof(string), booking.GetProviderClrType());
        Assert.Equal(typeof(string), payment.GetProviderClrType());
    }
}
