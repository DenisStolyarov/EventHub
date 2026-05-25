namespace EventHub.Api.Application.Errors;

internal static class EventServiceErrors
{
    public const string FromMustBeBeforeTo =
        "From date must be earlier than or equal to To date.";
}