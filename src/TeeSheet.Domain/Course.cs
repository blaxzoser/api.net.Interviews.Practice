namespace TeeSheet.Domain;

public class Course
{
    public int CourseId { get; set; }
    public required string Name { get; set; }

    /// <summary>IANA id, e.g. "America/Chicago" or "Europe/London".</summary>
    public required string TimeZoneId { get; set; }

    /// <summary>"US" or "UK".</summary>
    public required string Country { get; set; }

    /// <summary>"USD" or "GBP".</summary>
    public required string Currency { get; set; }

    public ICollection<TeeTime> TeeTimes { get; set; } = [];
}
