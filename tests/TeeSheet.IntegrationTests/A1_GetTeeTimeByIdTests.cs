using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TeeSheet.IntegrationTests.Infrastructure;

namespace TeeSheet.IntegrationTests;

/// <summary>
/// A1 — GET /api/teetimes/{id:int}. These tests are RED until you build the endpoint.
/// They only look at HTTP + JSON, so they don't depend on how you name your own types.
/// </summary>
[Collection(ApiCollection.Name)]
public class A1_GetTeeTimeByIdTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Shape of the response, declared here on purpose (not your TeeTimeDto).
    private sealed record TeeTimeResponse(
        int Id,
        string CourseName,
        DateTime StartUtc,
        string StartLocal,
        int SpotsLeft,
        decimal Price,
        string Currency);

    [Fact]
    public async Task Existing_tee_time_returns_200_with_the_documented_fields()
    {
        var id = await factory.SeedTeeTimeAsync(
            "Cedar Ridge GC", "America/Chicago", "USD",
            startUtc: new DateTime(2026, 11, 10, 14, 0, 0), bookedPlayers: 1, price: 55m);

        var response = await _client.GetAsync($"/api/teetimes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TeeTimeResponse>();
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.Equal("Cedar Ridge GC", body.CourseName);
        Assert.Equal(new DateTime(2026, 11, 10, 14, 0, 0, DateTimeKind.Utc), body.StartUtc);
        Assert.Equal(DateTimeKind.Utc, body.StartUtc.Kind);   // "Z" suffix: UTC must be explicit in the JSON
        Assert.Equal(3, body.SpotsLeft);                      // capacity 4 - booked 1
        Assert.Equal(55m, body.Price);
        Assert.Equal("USD", body.Currency);
    }

    // Wall-clock time of startLocal must follow the COURSE's time zone, not the server's or the browser's.
    [Theory]
    [InlineData("America/Chicago", "2026-11-10T14:00:00Z", 8, 0)]    // CST, UTC-6
    [InlineData("America/Phoenix", "2026-11-10T14:00:00Z", 7, 0)]    // no DST, UTC-7
    [InlineData("Europe/London", "2026-07-04T06:00:00Z", 7, 0)]      // BST, UTC+1
    [InlineData("Europe/London", "2026-03-29T00:30:00Z", 0, 30)]     // DST day, just BEFORE the clocks change (GMT)
    [InlineData("Europe/London", "2026-03-29T01:30:00Z", 2, 30)]     // DST day, just AFTER the clocks change (BST)
    public async Task StartLocal_is_expressed_in_the_course_time_zone(
        string timeZoneId, string startUtcIso, int expectedHour, int expectedMinute)
    {
        var startUtc = DateTime.Parse(startUtcIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        var id = await factory.SeedTeeTimeAsync(
            "TZ test course", timeZoneId, timeZoneId.StartsWith("Europe") ? "GBP" : "USD", startUtc);

        var body = await _client.GetFromJsonAsync<TeeTimeResponse>($"/api/teetimes/{id}");

        Assert.NotNull(body);
        // .DateTime keeps the wall-clock part whether you return "2026-11-10T08:00:00" or "2026-11-10T08:00:00-06:00".
        var wallClock = DateTimeOffset.Parse(body.StartLocal, CultureInfo.InvariantCulture).DateTime;
        Assert.Equal(startUtc.Date.AddHours(expectedHour).AddMinutes(expectedMinute), wallClock);
    }

    [Fact]
    public async Task Unknown_id_returns_404_with_ProblemDetails()
    {
        var response = await _client.GetAsync("/api/teetimes/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    // With the {id:int} route constraint "abc" doesn't match any route at all -> 404.
    // Without the constraint, model binding fails instead -> 400.
    [Fact]
    public async Task Non_numeric_id_does_not_match_the_route()
    {
        var response = await _client.GetAsync("/api/teetimes/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // One round trip: no N+1, no "load the course separately". Look at the SQL when this fails.
    [Fact]
    public async Task Reading_a_tee_time_runs_exactly_one_sql_command()
    {
        var id = await factory.SeedTeeTimeAsync(
            "Query count course", "Europe/London", "GBP", new DateTime(2026, 11, 10, 9, 0, 0));
        factory.SqlLog.Clear();

        var response = await _client.GetAsync($"/api/teetimes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(factory.SqlLog.Commands);
    }
}
