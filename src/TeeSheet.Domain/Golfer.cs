namespace TeeSheet.Domain;

public class Golfer
{
    public int GolferId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public bool IsMember { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}
