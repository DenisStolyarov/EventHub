using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Shared.Messaging;

namespace EventHub.Events.Infrastructure.Persistence.Repositories;

public sealed class InboxRepository(EventsDbContext context, TimeProvider timeProvider) : IInboxRepository
{
    public void Add(Guid id) =>
        context.InboxMessages.Add(new InboxMessage
        {
            Id = id,
            ReceivedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
}
