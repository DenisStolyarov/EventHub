using System.Text.Json;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Shared.Contracts;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Bookings.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository(BookingsDbContext context, TimeProvider timeProvider) : IOutboxRepository, IOutboxProcessor
{
    public void Add<TMessage>(TMessage message) where TMessage : IIntegrationEvent
    {
        (string topic, string key) = message switch
        {
            BookingCreated bookingCreated => (Topics.BookingCreated, bookingCreated.EventId.ToString()),
            BookingCancelled bookingCancelled => (Topics.BookingCancelled, bookingCancelled.EventId.ToString()),
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
