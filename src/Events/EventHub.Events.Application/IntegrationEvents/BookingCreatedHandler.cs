using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Domain.Entities;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventHub.Events.Application.IntegrationEvents;

public sealed class BookingCreatedHandler(
    IUnitOfWork unitOfWork,
    ILogger<BookingCreatedHandler> logger) : IIntegrationMessageHandler<BookingCreated>
{
    public async Task HandleAsync(BookingCreated message, CancellationToken cancellationToken = default)
    {
        Event? @event = await unitOfWork.Events.GetByIdAsync(message.EventId, cancellationToken);

        if (@event is null)
        {
            unitOfWork.Inbox.Add(message.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Event {EventId} not found for booking {BookingId}", message.EventId, message.BookingId);

            return;
        }

        if (@event.TryReserveSeats())
        {
            EventSeatReserved eventSeatReserved = new(Guid.CreateVersion7(), message.BookingId);

            unitOfWork.Outbox.Add(eventSeatReserved);
        }
        else
        {
            EventSeatUnavailable eventSeatUnavailable = new(Guid.CreateVersion7(), message.BookingId, "No Available Seats");

            unitOfWork.Outbox.Add(eventSeatUnavailable);
        }

        unitOfWork.Inbox.Add(message.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seats for booking {BookingId} are processed", message.BookingId);
    }
}
