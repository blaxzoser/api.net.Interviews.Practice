using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeeSheet.Domain;
using TeeSheet.IntegrationTests.Infrastructure;

namespace TeeSheet.IntegrationTests;

/// <summary>
/// A3 — POST /api/bookings against a real SQL Server, including the parallel booking scenario.
/// They only look at HTTP + JSON + the saved rows, so they don't depend on how you name your own types.
/// </summary>
[Collection(ApiCollection.Name)]
public class A3_CreateBookingTests(ApiFactory factory)
{
    private const string Url = "/api/bookings";

    private readonly HttpClient _client = factory.CreateClient();

    private Task<int> SeedTeeTimeInDaysAsync(double days, byte bookedPlayers = 0) =>
        factory.SeedTeeTimeAsync(
            "Booking test course", "America/Chicago", "USD",
            DateTime.UtcNow.AddDays(days), bookedPlayers);

    private Task<HttpResponseMessage> PostAsync(int teeTimeId, int golferId, int players) =>
        _client.PostAsJsonAsync(Url, new { teeTimeId, golferId, players });

    private Task<List<Booking>> BookingsForAsync(int teeTimeId) =>
        factory.WithDbAsync(db => db.Bookings.AsNoTracking().Where(b => b.TeeTimeId == teeTimeId).ToListAsync());

    private Task<byte> BookedPlayersForAsync(int teeTimeId) =>
        factory.WithDbAsync(db => db.TeeTimes.AsNoTracking()
            .Where(t => t.TeeTimeId == teeTimeId)
            .Select(t => t.BookedPlayers)
            .SingleAsync());

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
    }

    [Fact]
    public async Task Valid_request_returns_201_with_a_Location_header()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 2);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = Assert.Single(await BookingsForAsync(teeTimeId));
        Assert.Equal($"/api/bookings/{booking.BookingId}", response.Headers.Location?.ToString());
    }

    // R3: datetime2(0) rounds to whole seconds, so allow a little either side.
    [Fact]
    public async Task Booking_is_saved_as_Held_and_expires_in_10_minutes()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);
        var golferId = await factory.SeedGolferAsync(isMember: false);
        var before = DateTime.UtcNow;

        var response = await PostAsync(teeTimeId, golferId, players: 2);

        var after = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = Assert.Single(await BookingsForAsync(teeTimeId));
        Assert.Equal(golferId, booking.GolferId);
        Assert.Equal(2, booking.Players);
        Assert.Equal(BookingStatus.Held, booking.Status);
        Assert.NotNull(booking.HoldExpiresUtc);
        Assert.InRange(
            booking.HoldExpiresUtc.Value,
            before.AddMinutes(10).AddSeconds(-2),
            after.AddMinutes(10).AddSeconds(2));
    }

    // Must add to the existing count, not overwrite it.
    [Fact]
    public async Task Booking_adds_to_the_players_already_booked()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3, bookedPlayers: 1);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 2);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(3, await BookedPlayersForAsync(teeTimeId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Players_on_the_limits_of_the_range_are_accepted(int players)
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Players_outside_the_range_return_400_and_save_nothing(int players)
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Empty(await BookingsForAsync(teeTimeId));
        Assert.Equal(0, await BookedPlayersForAsync(teeTimeId));
    }

    [Fact]
    public async Task Unknown_tee_time_returns_404()
    {
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId: 999999999, golferId, players: 1);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tee_time_in_the_past_returns_404_and_saves_nothing()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(-1);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 1);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Empty(await BookingsForAsync(teeTimeId));
    }

    [Fact]
    public async Task Unknown_golfer_returns_404_and_saves_nothing()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);

        var response = await PostAsync(teeTimeId, golferId: 999999999, players: 1);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Empty(await BookingsForAsync(teeTimeId));
    }

    // R4 at 10 days ahead: inside the member window, outside the visitor window.
    [Fact]
    public async Task Member_can_book_10_days_ahead()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(10);
        var golferId = await factory.SeedGolferAsync(isMember: true);

        var response = await PostAsync(teeTimeId, golferId, players: 1);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Visitor_cannot_book_10_days_ahead()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(10);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 1);

        await AssertProblemAsync(response, (HttpStatusCode)422);
        Assert.Empty(await BookingsForAsync(teeTimeId));
    }

    [Fact]
    public async Task Member_cannot_book_20_days_ahead()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(20);
        var golferId = await factory.SeedGolferAsync(isMember: true);

        var response = await PostAsync(teeTimeId, golferId, players: 1);

        await AssertProblemAsync(response, (HttpStatusCode)422);
        Assert.Empty(await BookingsForAsync(teeTimeId));
    }

    // R1: 3 of 4 spots are taken, so 2 more don't fit. The booking must not be saved either.
    [Fact]
    public async Task Booking_that_does_not_fit_returns_409_and_saves_nothing()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3, bookedPlayers: 3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 2);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Empty(await BookingsForAsync(teeTimeId));
        Assert.Equal(3, await BookedPlayersForAsync(teeTimeId));
    }

    [Fact]
    public async Task Booking_that_exactly_fills_the_tee_time_succeeds()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3, bookedPlayers: 3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await PostAsync(teeTimeId, golferId, players: 1);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(4, await BookedPlayersForAsync(teeTimeId));
    }

    // The scenario from the exercise: read-check-write fails this, an atomic conditional UPDATE passes.
    [Fact]
    public async Task Ten_parallel_bookings_for_4_spots_give_exactly_4_successes_and_6_conflicts()
    {
        var teeTimeId = await SeedTeeTimeInDaysAsync(3);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => PostAsync(teeTimeId, golferId, players: 1)));

        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(6, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(4, await BookedPlayersForAsync(teeTimeId));
        Assert.Equal(4, (await BookingsForAsync(teeTimeId)).Count);
    }
}
