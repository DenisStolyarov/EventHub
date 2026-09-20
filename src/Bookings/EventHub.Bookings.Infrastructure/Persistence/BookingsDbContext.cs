using EventHub.Bookings.Domain.Entities;
using EventHub.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Bookings.Infrastructure.Persistence;

public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);

        modelBuilder.ApplyMessagingConfigurations();
    }
}
