using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Domain.Entities;
using EventHub.Events.Domain.Exceptions;
using EventHub.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace EventHub.Events.Application.IntegrationEvents;

public sealed class BookingCancelledHandler(
    IUnitOfWork unitOfWork,
    ILogger<BookingCancelledHandler> logger) : IIntegrationMessageHandler<BookingCancelled>
{
    public async Task HandleAsync(BookingCancelled message, CancellationToken cancellationToken = default)
    {
        Event? @event = await unitOfWork.Events.GetByIdAsync(message.EventId, cancellationToken);

        if (@event is null)
        {
            unitOfWork.Inbox.Add(message.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("Event {EventId} not found", message.EventId);

            return;
        }

        try
        {
            @event.ReleaseSeats();
        }
        catch (DomainException ex)
        {
            unitOfWork.Inbox.Add(message.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning(ex, "Seats for event {EventId} are already released or event capacity is exceeded, skipping", message.EventId);

            return;
        }

        unitOfWork.Inbox.Add(message.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seats for event {EventId} are released", message.EventId);
    }
}
