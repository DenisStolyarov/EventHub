namespace EventHub.Api.Domain.Exceptions;

public class DomainException : Exception
{
    public string Property { get; } = string.Empty;

    public DomainException() : base()
    { }

    public DomainException(string message) : base(message)
    { }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    { }

    public DomainException(string property, string message) : base(message) =>
        Property = property;

    public DomainException(string property, string message, Exception innerException)
        : base(message, innerException) =>
        Property = property;
}