namespace EventHub.Api.Application.Exceptions;

public class ValidationException : Exception
{
    private const string DefaultMessage = "One or more validation errors occurred.";

    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base() =>
        Errors = new Dictionary<string, string[]>();

    public ValidationException(string message) : base(message) =>
        Errors = new Dictionary<string, string[]>();

    public ValidationException(string message, Exception innerException) : base(message, innerException) =>
        Errors = new Dictionary<string, string[]>();

    public ValidationException(IDictionary<string, string[]> errors)
        : base(DefaultMessage) =>
        Errors = new Dictionary<string, string[]>(errors);

    public ValidationException(string property, string error)
        : base(DefaultMessage) =>
        Errors = new Dictionary<string, string[]>
        {
            [property] = [error]
        };

    public ValidationException(IDictionary<string, string[]> errors, string message, Exception innerException)
        : base(message, innerException) =>
        Errors = new Dictionary<string, string[]>(errors);
}