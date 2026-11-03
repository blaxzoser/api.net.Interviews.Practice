using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeeSheet.Domain;
using TeeSheet.Dto;
using TeeSheet.Infrastructure.Persistence;

namespace TeeSheet.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("bookings");

        // -----------------------------------------------------------------------------
        // A3 — POST create booking and UPDATE tee time (75 min) ★★ most likely live task
        // Build: POST /api/bookings with CreateBookingRequest { teeTimeId, golferId, players }.
        // Request body: { "teeTimeId": 42, "golferId": 7, "players": 3 }
        // players is an int from 1 to 4: how many spots to reserve, not an array of golfer ids.
        // Acceptance:

        // Validation: players 1–4; tee time exists; tee time is in the future; R4 (members 14 days / visitors 7 days)
        // R4 is the advance booking window. Load the golfer (404 if not found); Golfers.IsMember picks the limit: true = 14 days, false = 7 days.
        // Allowed when teeTime.StartUtc <= nowUtc.AddDays(limit). Otherwise return 422 with ProblemDetails.
        // Two writes in one transaction: INSERT the new row into Bookings, and UPDATE the existing TeeTimes row to add the players to BookedPlayers. If either fails, neither is saved.
        // R1 enforced safely under concurrency when reserving the spots on TeeTimes. Pick one approach and explain it:
        // (a) atomic conditional UPDATE ... WHERE BookedPlayers + @p <= Capacity, check rows affected, or
        // (b) EF Core optimistic concurrency on RowVersion + retry, or
        // (c) a transaction with UPDLOCK, ROWLOCK hint
        // The inserted booking has status Held and HoldExpiresUtc = now + 10 min (R3)
        // Returns 201 Created with a Location header pointing to GET /api/bookings/{id}
        // 409 with ProblemDetails when full
        // Test to write: an integration test that fires 10 parallel POSTs for 1 player each at a tee time with capacity 4. Assert exactly 4 succeed and 6 get 409. This is the test that impresses. Follow-ups: Why is "read, check, then write" a race? Which isolation level would make it safe, and what would that cost? How do you handle DbUpdateConcurrencyException?
        // -----------------------------------------------------------------------------
         group.MapPost("/", CreateBookingRequest);    

        return app;
    }

    static async Task<Results<Created, ProblemHttpResult>> CreateBookingRequest(
        BookingRequestDto bookingRequest, TeeSheetDbContext db, CancellationToken ct)
    {


      var now = DateTime.UtcNow;

      if (!BookingRules.IsValidPlayers(bookingRequest.Players))
      {
               return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "there are not enought  players, it should be 1 to 4",
                detail: "");
      }


        var selected = await db.TeeTimes
                    .AsNoTracking()
                    .Where(source => source.TeeTimeId == bookingRequest.TeeTimeId)
                    .FirstOrDefaultAsync(ct);;   
        
        if(selected == null || !BookingRules.IsInFuture(selected.StartUtc, now))
            return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "there is not existed TeeTimeId or its not on future ",
            detail: "");                 
        

        var  isExistGolder = await db.Golfers.AsNoTracking()
                            .Where(source => source.GolferId == bookingRequest.GolferId)
                            .FirstOrDefaultAsync(ct);;   
              
        if(isExistGolder == null)
            return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "there is not Golder",
            detail: "");      

        if(!BookingRules.IsWithinBookingWindow(selected.StartUtc, isExistGolder.IsMember, now))
                        return TypedResults.Problem(
                        statusCode: StatusCodes.Status422UnprocessableEntity,
                        title: "limit is pass away",
                        detail: "");  
        
        
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var newBooking = new Booking
        {
            GolferId = bookingRequest.GolferId,
            TeeTimeId = bookingRequest.TeeTimeId,
            CreatedUtc = now,
            Players = bookingRequest.Players,
            Status = BookingStatus.Held,
            HoldExpiresUtc = BookingRules.HoldExpiresUtc(now)
        };
        await db.Bookings.AddAsync(newBooking,ct);

        var updated = await db.TeeTimes
            .Where(t => t.TeeTimeId == bookingRequest.TeeTimeId
                    && t.BookedPlayers + bookingRequest.Players <= t.Capacity)
            .ExecuteUpdateAsync(s => s.SetProperty(
                t => t.BookedPlayers,
                t => (byte)(t.BookedPlayers + bookingRequest.Players)), ct);

        if (updated == 0)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tee time is full");

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return TypedResults.Created($"/api/bookings/{newBooking.BookingId}");
    }
}
