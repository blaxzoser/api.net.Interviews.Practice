namespace TeeSheet.Domain;

/// <summary>Booking rules with no database and no clock, so every limit can be unit tested exactly.</summary>
public static class BookingRules
{
    public const int MinPlayers = 1;
    public const int MaxPlayers = 4;
    public const int MemberWindowDays = 14;
    public const int VisitorWindowDays = 7;
    public const int HoldMinutes = 10;

    public static bool IsValidPlayers(byte players) => players is >= MinPlayers and <= MaxPlayers;

    public static bool IsInFuture(DateTime startUtc, DateTime nowUtc) => startUtc > nowUtc;

    /// <summary>R4: members can book 14 days ahead, visitors 7. Exactly on the limit is allowed.</summary>
    public static bool IsWithinBookingWindow(DateTime startUtc, bool isMember, DateTime nowUtc) =>
        startUtc <= nowUtc.AddDays(isMember ? MemberWindowDays : VisitorWindowDays);

    /// <summary>R3: a held booking expires 10 minutes after it is created.</summary>
    public static DateTime HoldExpiresUtc(DateTime nowUtc) => nowUtc.AddMinutes(HoldMinutes);
}
