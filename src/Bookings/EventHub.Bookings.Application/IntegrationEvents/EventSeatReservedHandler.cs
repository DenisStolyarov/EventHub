using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventHub.Bookings.Application.IntegrationEvents;

public sealed class EventSeatReservedHandler(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EventSeatReservedHandler> logger) : IIntegrationMessageHandler<EventSeatReserved>
{
    public async Task HandleAsync(EventSeatReserved message, CancellationToken cancellationToken = default)
    {
        Booking? booking = await unitOfWork.Bookings.GetByIdAsync(message.BookingId, cancellationToken);

        if (booking is null)
        {
            unitOfWork.Inbox.Add(message.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Booking {BookingId} not found", message.BookingId);

            return;
        }

        if (booking.Status is BookingStatus.Cancelled)
        {
            BookingCancelled bookingCancelled = new(
                Guid.CreateVersion7(),
                booking.EventId);

            unitOfWork.Inbox.Add(message.Id);
            unitOfWork.Outbox.Add(bookingCancelled);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Booking {BookingId} is cancelled, compensating", booking.Id);

            return;
        }

        booking.Confirm(timeProvider.GetUtcNow().UtcDateTime);

        unitOfWork.Inbox.Add(message.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} is confirmed", booking.Id);
    }
}
