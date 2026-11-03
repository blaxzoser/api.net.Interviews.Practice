using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeeSheet.Domain;
using TeeSheet.IntegrationTests.Infrastructure;

namespace TeeSheet.IntegrationTests;

/// <summary>
/// A4 — PATCH /api/bookings/{id} with { players }. Each test creates its booking through POST,
/// then checks both tables: the booking's own Players and the tee time's BookedPlayers.
/// </summary>
[Collection(ApiCollection.Name)]
public class A4_ChangePlayersTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(int TeeTimeId, int BookingId)> BookAsync(int players, byte alreadyBooked = 0)
    {
        var teeTimeId = await factory.SeedTeeTimeAsync(
            "Change players course", "America/Chicago", "USD", DateTime.UtcNow.AddDays(3), alreadyBooked);
        var golferId = await factory.SeedGolferAsync(isMember: false);

        var response = await _client.PostAsJsonAsync("/api/bookings", new { teeTimeId, golferId, players });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var bookingId = int.Parse(response.Headers.Location!.ToString().Split('/').Last());
        return (teeTimeId, bookingId);
    }

    private Task<HttpResponseMessage> PatchAsync(int bookingId, int players) =>
        _client.PatchAsJsonAsync($"/api/bookings/{bookingId}", new { players });

    private Task<(byte BookedPlayers, byte BookingPlayers)> StateAsync(int teeTimeId, int bookingId) =>
        factory.WithDbAsync(async db => (
            await db.TeeTimes.AsNoTracking().Where(t => t.TeeTimeId == teeTimeId).Select(t => t.BookedPlayers).SingleAsync(),
            await db.Bookings.AsNoTracking().Where(b => b.BookingId == bookingId).Select(b => b.Players).SingleAsync()));

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(1, 4)]
    public async Task Increasing_players_returns_204_and_updates_both_tables(int from, int to)
    {
        var (teeTimeId, bookingId) = await BookAsync(from);

        var response = await PatchAsync(bookingId, to);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(((byte)to, (byte)to), await StateAsync(teeTimeId, bookingId));
    }

    // A negative difference once crashed with OverflowException (a byte parameter can't hold -2).
    [Theory]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    public async Task Reducing_players_returns_204_and_releases_the_spots(int from, int to)
    {
        var (teeTimeId, bookingId) = await BookAsync(from);

        var response = await PatchAsync(bookingId, to);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(((byte)to, (byte)to), await StateAsync(teeTimeId, bookingId));
    }

    // Changes by the difference: the other booking's 1 player stays in the total.
    [Fact]
    public async Task Other_bookings_stay_in_the_tee_time_total()
    {
        var (teeTimeId, bookingId) = await BookAsync(3, alreadyBooked: 1);

        var response = await PatchAsync(bookingId, 1);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(((byte)2, (byte)1), await StateAsync(teeTimeId, bookingId));
    }

    [Fact]
    public async Task Same_number_of_players_returns_204_and_changes_nothing()
    {
        var (teeTimeId, bookingId) = await BookAsync(2, alreadyBooked: 2);

        var response = await PatchAsync(bookingId, 2);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(((byte)4, (byte)2), await StateAsync(teeTimeId, bookingId));
    }

    // R1: 2 other players + 3 = 5, over capacity 4. Neither table may change.
    [Fact]
    public async Task Change_that_does_not_fit_returns_409_and_changes_nothing()
    {
        var (teeTimeId, bookingId) = await BookAsync(2, alreadyBooked: 2);

        var response = await PatchAsync(bookingId, 3);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal(((byte)4, (byte)2), await StateAsync(teeTimeId, bookingId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Players_outside_the_range_return_400_and_change_nothing(int players)
    {
        var (teeTimeId, bookingId) = await BookAsync(2);

        var response = await PatchAsync(bookingId, players);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(((byte)2, (byte)2), await StateAsync(teeTimeId, bookingId));
    }

    [Fact]
    public async Task Unknown_booking_returns_404()
    {
        var response = await PatchAsync(999999999, 2);

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelled_booking_returns_409_and_changes_nothing()
    {
        var (teeTimeId, bookingId) = await BookAsync(2);
        await factory.WithDbAsync(db => db.Bookings
            .Where(b => b.BookingId == bookingId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Cancelled)));

        var response = await PatchAsync(bookingId, 3);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal(((byte)2, (byte)2), await StateAsync(teeTimeId, bookingId));
    }
}
