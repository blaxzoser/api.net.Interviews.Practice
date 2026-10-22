using Microsoft.EntityFrameworkCore;
using TeeSheet.Infrastructure.Persistence;

namespace TeeSheet.Api.Endpoints;

/// <summary>Development-only helpers. Not part of any exercise.</summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // Row counts per table: quick proof that the connection string, schema and EF mapping all work.
        app.MapGet("/dev/stats", async (TeeSheetDbContext db, CancellationToken ct) => new
        {
            courses = await db.Courses.CountAsync(ct),
            teeTimes = await db.TeeTimes.CountAsync(ct),
            golfers = await db.Golfers.CountAsync(ct),
            bookings = await db.Bookings.CountAsync(ct),
            payments = await db.Payments.CountAsync(ct)
        }).ExcludeFromDescription();

        return app;
    }
}
