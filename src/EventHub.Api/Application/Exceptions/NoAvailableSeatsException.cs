namespace EventHub.Api.Application.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException() : base("No available seats for this event.")
    { }

    public NoAvailableSeatsException(string message) : base(message)
    { }

    public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException)
    { }
}
