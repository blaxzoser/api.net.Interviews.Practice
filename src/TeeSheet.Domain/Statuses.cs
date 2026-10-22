namespace TeeSheet.Domain;

/// <summary>Stored as text in the database (VARCHAR), so the names below are the stored values.</summary>
public enum BookingStatus
{
    Held,
    Confirmed,
    Cancelled
}

/// <summary>Stored as text in the database (VARCHAR), so the names below are the stored values.</summary>
public enum PaymentStatus
{
    Pending,
    Captured,
    Failed,
    Refunded
}
