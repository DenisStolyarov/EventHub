using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Shared.Messaging;

namespace EventHub.Bookings.Infrastructure.Persistence.Repositories;

public sealed class InboxRepository(BookingsDbContext context, TimeProvider timeProvider) : IInboxRepository
{
    public void Add(Guid id) =>
        context.InboxMessages.Add(new InboxMessage
        {
            Id = id,
            ReceivedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
}
