namespace EventHub.Api.Application.Exceptions;

public class NotFoundException : Exception
{
    public object EntityId { get; } = string.Empty;

    public string EntityName { get; } = string.Empty;

    public NotFoundException() : base()
    { }

    public NotFoundException(string message) : base(message)
    { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    { }

    public NotFoundException(string entityName, object id)
        : base($"'{entityName}' with id '{id}' was not found.")
    {
        EntityId = id;
        EntityName = entityName;
    }

    public NotFoundException(string entityName, object id, Exception innerException)
        : base($"'{entityName}' with key '{id}' was not found.", innerException)
    {
        EntityId = id;
        EntityName = entityName;
    }
}