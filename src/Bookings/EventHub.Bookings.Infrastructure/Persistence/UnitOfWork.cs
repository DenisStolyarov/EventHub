using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Bookings.Infrastructure.Persistence.Repositories;

namespace EventHub.Bookings.Infrastructure.Persistence;

public sealed class UnitOfWork(BookingsDbContext context) : IUnitOfWork
{
    private readonly Lazy<IBookingRepository> _bookings = new(() => new BookingRepository(context));

    public IBookingRepository Bookings => _bookings.Value;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
