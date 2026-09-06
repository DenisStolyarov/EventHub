namespace EventHub.Domain.Exceptions;

public class MaxActiveBookingsExceededException : Exception
{
    public MaxActiveBookingsExceededException() : base("Active bookings limit reached.")
    { }

    public MaxActiveBookingsExceededException(int maxActiveBookings)
        : base($"Active bookings limit ({maxActiveBookings}) reached.")
    { }

    public MaxActiveBookingsExceededException(string message) : base(message)
    { }

    public MaxActiveBookingsExceededException(string message, Exception innerException) : base(message, innerException)
    { }
}
