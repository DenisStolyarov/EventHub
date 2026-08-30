namespace EventHub.Application.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("User is not authenticated.")
    { }

    public UnauthorizedException(string message) : base(message)
    { }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    { }
}