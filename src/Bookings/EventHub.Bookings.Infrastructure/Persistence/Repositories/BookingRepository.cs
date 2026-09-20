using System.Linq.Expressions;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Bookings.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(BookingsDbContext context) : IBookingRepository, IBookingCounter
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<ICollection<Booking>> GetActiveByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Where(b => b.EventId == eventId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync(cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);

    public async Task<int> CountAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) =>
        await context.Bookings.CountAsync(predicate, cancellationToken);
}
