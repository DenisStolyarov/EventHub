using System.Text.Json;
using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Shared.Contracts;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Events.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository(EventsDbContext context, TimeProvider timeProvider) : IOutboxRepository, IOutboxProcessor
{
    public void Add<TMessage>(TMessage message) where TMessage : IIntegrationEvent
    {
        (string topic, string key) = message switch
        {
            EventSeatReserved eventSeatReserved => (Topics.EventSeatReserved, eventSeatReserved.BookingId.ToString()),
            EventSeatUnavailable eventSeatUnavailable => (Topics.EventSeatUnavailable, eventSeatUnavailable.BookingId.ToString()),
            EventCancelled eventCancelled => (Topics.EventCancelled, eventCancelled.EventId.ToString()),
            _ => throw new InvalidOperationException($"Unsupported integration event type: {typeof(TMessage).Name}"),
        };

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = message.Id,
            Topic = topic,
            Key = key,
            Payload = JsonSerializer.Serialize(message),
            OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
        });
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .ThenBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task MarkProcessedAsync(IReadOnlyCollection<OutboxMessage> messages, DateTime processedAt, CancellationToken cancellationToken = default)
    {
        foreach (OutboxMessage message in messages)
        {
            message.ProcessedAt = processedAt;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
