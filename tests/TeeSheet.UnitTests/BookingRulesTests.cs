using TeeSheet.Domain;

namespace TeeSheet.UnitTests;

/// <summary>
/// A3 — the rules that need no database. "now" is fixed, so the exact limits can be tested.
/// R1 (capacity) lives in the SQL UPDATE, so it is covered by A3_CreateBookingTests instead.
/// </summary>
public class BookingRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Players_from_1_to_4_are_valid(byte players)
    {
        Assert.True(BookingRules.IsValidPlayers(players));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(255)]
    public void Players_outside_1_to_4_are_invalid(byte players)
    {
        Assert.False(BookingRules.IsValidPlayers(players));
    }

    [Fact]
    public void Tee_time_after_now_is_in_the_future()
    {
        Assert.True(BookingRules.IsInFuture(Now.AddSeconds(1), Now));
    }

    [Fact]
    public void Tee_time_exactly_now_is_not_in_the_future()
    {
        Assert.False(BookingRules.IsInFuture(Now, Now));
    }

    [Fact]
    public void Tee_time_before_now_is_not_in_the_future()
    {
        Assert.False(BookingRules.IsInFuture(Now.AddSeconds(-1), Now));
    }

    // R4: exactly on the limit is allowed, one second later is not.
    [Theory]
    [InlineData(true, 14)]
    [InlineData(false, 7)]
    public void Tee_time_exactly_on_the_window_limit_is_allowed(bool isMember, int days)
    {
        Assert.True(BookingRules.IsWithinBookingWindow(Now.AddDays(days), isMember, Now));
    }

    [Theory]
    [InlineData(true, 14)]
    [InlineData(false, 7)]
    public void Tee_time_just_past_the_window_limit_is_rejected(bool isMember, int days)
    {
        Assert.False(BookingRules.IsWithinBookingWindow(Now.AddDays(days).AddSeconds(1), isMember, Now));
    }

    // The scenario that tells the two limits apart: 10 days ahead.
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Ten_days_ahead_is_allowed_for_members_only(bool isMember, bool expected)
    {
        Assert.Equal(expected, BookingRules.IsWithinBookingWindow(Now.AddDays(10), isMember, Now));
    }

    // A4: players are validated with IsValidPlayers above; this is the status rule.
    [Theory]
    [InlineData(BookingStatus.Held, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public void Only_bookings_that_are_not_cancelled_can_change_players(BookingStatus status, bool expected)
    {
        Assert.Equal(expected, BookingRules.CanChangePlayers(status));
    }

    [Fact]
    public void Hold_expires_10_minutes_after_now()
    {
        Assert.Equal(new DateTime(2026, 11, 2, 10, 10, 0, DateTimeKind.Utc), BookingRules.HoldExpiresUtc(Now));
    }
}
