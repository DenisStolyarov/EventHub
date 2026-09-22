using EventHub.Bookings.Domain.Entities;

namespace EventHub.Bookings.IntegrationTests.TestData;

public static class EntityProvider
{
    private static readonly DateTime DefaultCreatedAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    public static Booking CreateBooking(Guid eventId, Guid? userId = null, DateTime? createdAt = null) =>
        new(Guid.CreateVersion7(), eventId, userId ?? Guid.NewGuid(), createdAt ?? DefaultCreatedAt);
}
