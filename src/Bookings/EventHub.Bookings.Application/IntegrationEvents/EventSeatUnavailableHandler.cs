using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventHub.Bookings.Application.IntegrationEvents;

public sealed class EventSeatUnavailableHandler(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<EventSeatUnavailableHandler> logger) : IIntegrationMessageHandler<EventSeatUnavailable>
{
    public async Task HandleAsync(EventSeatUnavailable message, CancellationToken cancellationToken = default)
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
            unitOfWork.Inbox.Add(message.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Booking {BookingId} is cancelled, no compensation needed", booking.Id);

            return;
        }

        booking.Reject(timeProvider.GetUtcNow().UtcDateTime);

        unitOfWork.Inbox.Add(message.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Booking {BookingId} is rejected: {Reason}", booking.Id, message.Reason);
    }
}
