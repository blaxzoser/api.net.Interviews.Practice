namespace TeeSheet.Domain;

public class Payment
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
}
