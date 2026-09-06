namespace EventHub.Domain.Exceptions;

public class EventAlreadyStartedException : Exception
{
    public EventAlreadyStartedException() : base("Event has already started.")
    { }

    public EventAlreadyStartedException(string message) : base(message)
    { }

    public EventAlreadyStartedException(string message, Exception innerException) : base(message, innerException)
    { }
}
