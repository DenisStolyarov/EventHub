using System.ComponentModel.DataAnnotations;

namespace EventHub.Bookings.Api.Presentation.Models.Bookings;

public sealed record CreateBookingRequest : IValidatableObject
{
    public required Guid EventId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EventId == Guid.Empty)
        {
            yield return new ValidationResult("EventId cannot be empty.", [nameof(EventId)]);
        }
    }
}
