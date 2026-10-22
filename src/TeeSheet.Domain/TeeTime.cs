namespace TeeSheet.Domain;

public class TeeTime
{
    public int TeeTimeId { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    /// <summary>Always UTC. Convert with <see cref="Course.TimeZoneId"/> for display and local-time rules.</summary>
    public DateTime StartUtc { get; set; }

    public byte Capacity { get; set; } = 4;
    public byte BookedPlayers { get; set; }
    public decimal Price { get; set; }

    /// <summary>SQL Server rowversion, used for optimistic concurrency.</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<Booking> Bookings { get; set; } = [];
}
