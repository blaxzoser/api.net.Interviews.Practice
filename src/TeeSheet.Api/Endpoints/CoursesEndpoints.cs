using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TeeSheet.Dto;
using TeeSheet.Infrastructure.Persistence;

namespace TeeSheet.Api.Endpoints;

/// <summary>Development-only helpers. Not part of any exercise.</summary>
public static class CourseEndPoints
{
    public static IEndpointRouteBuilder MapBookEndPoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/course").WithTags("course");
        group.MapGet("/{id:int}/teetimes", GetByIdForTeetimes);
        return app;
    }

    // A2: GET /api/course/{id}/teetimes?date=2026-11-10&minSpots=2&sort=price&page=1&pageSize=20
    // Query-string values bind by parameter name, so the route template only holds the path.
    // REVIEW LATER: page/pageSize/minSpots are non-nullable ints, so leaving one off returns 400.
    // Make them `int? page = null` (or give them defaults) if they should be optional.
    static async Task<Results<Ok<PagedResult<TeeTimeDto>>, ProblemHttpResult>> GetByIdForTeetimes(
        int id, string date, int minSpots, string? sort, int page, int pageSize,
        TeeSheetDbContext db, CancellationToken ct)
    {
        // Load the course first: it gives the 404 and the TimeZoneId needed to turn `date` into a UTC range.
        // A query over db.TeeTimes can't tell "course missing" apart from "no tee times that day".
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.CourseId == id)
            .Select(c => new { c.TimeZoneId, c.Currency })
            .SingleOrDefaultAsync(ct);

        if (course is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Course not found",
                detail: $"No course with id {id}.");
        }

        // `date` is the course-local calendar day, so the window is [local midnight, next local midnight)
        // converted to UTC. Midnight is never inside a DST gap for US/UK zones, so this local->UTC step is safe.
        // REVIEW LATER: ParseExact throws on bad input, which returns a 500. Use TryParseExact and return a 400.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(course.TimeZoneId);
        var localDate = DateOnly.ParseExact(date, "yyyy-MM-dd");
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), tz);

        // Sargable: compare the raw StartUtc column against two constants so SQL can seek an index on
        // (CourseId, StartUtc). `t.StartUtc.Date == x` would wrap the column in a function and force a scan.
        // Still an IQueryable here: nothing has run yet, and every Where/OrderBy/Skip/Take becomes SQL.
        var query = db.TeeTimes.AsNoTracking()
            .Where(t => t.CourseId == id
                     && t.StartUtc >= startUtc && t.StartUtc < endUtc
                     && t.Capacity - t.BookedPlayers >= minSpots);

        // Whitelist the sort values instead of building the ORDER BY from a string, and reject anything else with a 400.
        IOrderedQueryable<Domain.TeeTime>? sorted = sort switch
        {
            null or "" or "time" => query.OrderBy(t => t.StartUtc),
            "price" => query.OrderBy(t => t.Price),
            _ => null
        };

        if (sorted is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid sort",
                detail: $"sort must be 'time' or 'price', got '{sort}'.");
        }

        // ---- Paging (offset pagination) ----
        // 1) Normalize input. Clamp pageSize to [1, 100] (AC cap) so a client can't ask for 1,000,000 rows,
        //    and floor page at 1 so (page - 1) * pageSize is never negative. Clamp instead of a 400
        //    is a choice: it's forgiving for clients. Returning 400 for page < 1 is just as valid if you say why.
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        // 2) totalCount comes from the filtered query, before Skip/Take, so it's the size of the whole result set
        //    and not just this page. It costs a second round trip (SELECT COUNT(*)). The AC requires totalCount;
        //    without that requirement, "take pageSize + 1" skips the COUNT (see cheatsheet A2).
        var totalCount = await query.CountAsync(ct);

        // 3) ThenBy(TeeTimeId) is the tiebreaker. Many tee times share a price, and SQL doesn't guarantee an order
        //    among ties, so without a unique last key one row can appear on page 1 and page 2, or on neither.
        // 4) Skip((page - 1) * pageSize).Take(pageSize) becomes OFFSET n ROWS FETCH NEXT m ROWS ONLY.
        //    Page 3 of size 20 skips 40 rows and takes rows 41-60. SQL still reads and discards every skipped row,
        //    so deep pages get slower. Keyset pagination fixes that (stretch goal, see cheatsheet A2).
        // 5) Project with Select before ToListAsync so only the DTO columns come back, and only one page of them.
        var items = await sorted.ThenBy(t => t.TeeTimeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeeTimeDto
            {
                Id = t.TeeTimeId,
                StartUtc = t.StartUtc,
                SpotsLeft = t.Capacity - t.BookedPlayers,
                Price = t.Price,
                Currency = course.Currency
            })
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TeeTimeDto>(items, page, pageSize, totalCount));
    }
}
