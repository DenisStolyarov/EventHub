using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Domain.Entities;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventHub.Bookings.Application.IntegrationEvents;

public sealed class EventCancelledHandler(
    IUnitOfWork unitOfWork,
    ILogger<EventCancelledHandler> logger) : IIntegrationMessageHandler<EventCancelled>
{
    public async Task HandleAsync(EventCancelled message, CancellationToken cancellationToken = default)
    {
        ICollection<Booking> bookings = await unitOfWork.Bookings.GetActiveByEventIdAsync(message.EventId, cancellationToken);

        foreach (Booking booking in bookings)
        {
            booking.Cancel();
        }

        unitOfWork.Inbox.Add(message.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Event {EventId} is cancelled, {Count} bookings are cancelled", message.EventId, bookings.Count);
    }
}
