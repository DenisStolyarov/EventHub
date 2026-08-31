using EventHub.Domain.Entities;
using EventHub.Domain.ValueObjects;

namespace EventHub.IntegrationTests.TestData;

public static class EntityProvider
{
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEnd = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultCreatedAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    public static Event CreateEvent(
        string title = "Test Event",
        string? description = "Description",
        int totalSeats = 100,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        DateTime start = startAt ?? DefaultStart;
        DateTime end = endAt ?? DefaultEnd;

        return new Event(Guid.CreateVersion7(), title, description, totalSeats, new Period(start, end));
    }

    public static Booking CreateBooking(Guid eventId, Guid? userId = null, DateTime? createdAt = null) =>
        new(Guid.CreateVersion7(), eventId, userId ?? Guid.NewGuid(), createdAt ?? DefaultCreatedAt);
}
