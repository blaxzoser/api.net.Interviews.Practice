namespace TeeSheet.Domain;

public class Booking
{
    public int BookingId { get; set; }

    public int TeeTimeId { get; set; }
    public TeeTime TeeTime { get; set; } = null!;

    public int GolferId { get; set; }
    public Golfer Golfer { get; set; } = null!;

    /// <summary>1 to 4.</summary>
    public byte Players { get; set; }

    public BookingStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? HoldExpiresUtc { get; set; }
    public string? IdempotencyKey { get; set; }

    public ICollection<Payment> Payments { get; set; } = [];
}
