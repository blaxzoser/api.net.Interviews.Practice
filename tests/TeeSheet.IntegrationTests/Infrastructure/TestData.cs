using TeeSheet.Domain;

namespace TeeSheet.IntegrationTests.Infrastructure;

public static class TestData
{
    /// <summary>Inserts a course with one tee time (capacity 4) and returns the new tee time id.</summary>
    public static Task<int> SeedTeeTimeAsync(
        this ApiFactory factory,
        string courseName,
        string timeZoneId,
        string currency,
        DateTime startUtc,
        byte bookedPlayers = 0,
        decimal price = 55m)
    {
        return factory.WithDbAsync(async db =>
        {
            var course = new Course
            {
                Name = courseName,
                TimeZoneId = timeZoneId,
                Country = currency == "GBP" ? "UK" : "US",
                Currency = currency
            };

            var teeTime = new TeeTime
            {
                Course = course,
                StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                Capacity = 4,
                BookedPlayers = bookedPlayers,
                Price = price
            };

            db.TeeTimes.Add(teeTime);
            await db.SaveChangesAsync();
            return teeTime.TeeTimeId;
        });
    }
}
