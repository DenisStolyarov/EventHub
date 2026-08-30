using System.Linq.Expressions;
using EventHub.Application.Abstractions.Persistence.Repositories;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(AppDbContext context) : IBookingRepository, IBookingCounter
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<ICollection<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);

    public async Task<int> CountAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) =>
        await context.Bookings.CountAsync(predicate, cancellationToken);
}
