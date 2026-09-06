namespace EventHub.Application.Exceptions;

public sealed class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException() : base("User with this login already exists.")
    { }

    public UserAlreadyExistsException(string message) : base(message)
    { }

    public UserAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
    { }
}
