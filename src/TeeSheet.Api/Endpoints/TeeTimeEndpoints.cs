using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeeSheet.Dto;
using TeeSheet.Infrastructure.Persistence;

namespace TeeSheet.Api.Endpoints;

public static class TeeTimeEndpoints
{
    public static IEndpointRouteBuilder MapTeeTimeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teetimes").WithTags("TeeTimes");

        // -----------------------------------------------------------------------------
        // A1 - GET /api/teetimes/{id:int}                       (15 min, Pass 1: no AI)
        //
        //   Returns TeeTimeDto { id, courseName, startUtc, startLocal, spotsLeft, price, currency }
        //   Acceptance: see README.md -> "A1" and
        //   tests/TeeSheet.IntegrationTests/A1_GetTeeTimeByIdTests.cs (red until you finish).
        //
        //   Start with the 2-minute opening talk track, then write it here, out loud.
        // -----------------------------------------------------------------------------
         group.MapGet("/{id:int}", GetById);    

        return app;
    }

    static async Task<Results<Ok<TeeTimeDto>, ProblemHttpResult>> GetById(
        int id, TeeSheetDbContext db, CancellationToken ct)
    {
        var row  = await db.TeeTimes.AsNoTracking()
         .Where(t => t.TeeTimeId == id)
         .Select(t => new { t.TeeTimeId, t.StartUtc, t.Price,
                       CourseName = t.Course.Name, t.Course.TimeZoneId, t.Course.Currency ,  calculateSpotsLeft = t.Capacity - t.BookedPlayers, })
        .FirstOrDefaultAsync(ct);
        
        if (row is null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Tee time not found",
                detail: $"No tee time with id {id}.");

        var utc   = DateTime.SpecifyKind(row.StartUtc, DateTimeKind.Utc);
        var tz    = TimeZoneInfo.FindSystemTimeZoneById(row.TimeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

        return TypedResults.Ok(new TeeTimeDto{ 
                    CourseName = row.CourseName ,
                    Id = row.TeeTimeId,
                    Currency = row.Currency,
                    Price = row.Price,
                    StartLocal = local,
                    StartUtc = utc,
                    SpotsLeft = row.calculateSpotsLeft

        });
    }
}
