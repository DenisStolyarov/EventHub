using System.Linq.Expressions;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Bookings.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(BookingsDbContext context) : IBookingRepository, IBookingCounter
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);

    public async Task<int> CountAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) =>
        await context.Bookings.CountAsync(predicate, cancellationToken);
}
