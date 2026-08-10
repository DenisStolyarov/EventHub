using EventHub.Api.Infrastructure.DataAccess;
using EventHub.Application.Abstractions.Persistence.Repositories;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Infrastructure.Repositories;

public sealed class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings.FindAsync([id], cancellationToken);

    public async Task<ICollection<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

    public void Add(Booking booking) => context.Bookings.Add(booking);
}