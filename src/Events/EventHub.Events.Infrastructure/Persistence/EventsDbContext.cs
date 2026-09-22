using EventHub.Events.Domain.Entities;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Events.Infrastructure.Persistence;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);

        modelBuilder.ApplyMessagingConfigurations();
    }
}
