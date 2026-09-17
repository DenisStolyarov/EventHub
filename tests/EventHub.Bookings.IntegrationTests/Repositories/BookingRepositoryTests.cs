using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;
using EventHub.Bookings.Infrastructure.Persistence;
using EventHub.Bookings.Infrastructure.Persistence.Repositories;
using EventHub.Bookings.IntegrationTests.Abstractions;
using EventHub.Bookings.IntegrationTests.Fixtures;
using EventHub.Bookings.IntegrationTests.TestData;
using FluentAssertions;

namespace EventHub.Bookings.IntegrationTests.Repositories;

public class BookingRepositoryTests(IntegrationTestFixture fixture) : RepositoryTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetByIdAsync_ReturnsBooking_WhenExists()
    {
        // Arrange
        await using BookingsDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Booking booking = EntityProvider.CreateBooking(eventId, userId);

        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Booking? result = await uow.Bookings.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(eventId);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        Booking? result = await uow.Bookings.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Add_PersistsBooking()
    {
        // Arrange
        await using BookingsDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Booking booking = EntityProvider.CreateBooking(eventId, userId);

        // Act
        uow.Bookings.Add(booking);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using BookingsDbContext verifyCtx = Fixture.CreateContext();

        Booking? stored = await verifyCtx.Bookings.FindAsync([booking.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.EventId.Should().Be(eventId);
        stored.UserId.Should().Be(userId);
        stored.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CountAsync_ReturnsCountMatchingPredicate()
    {
        // Arrange
        await using BookingsDbContext ctx = Fixture.CreateContext();
        IBookingCounter counter = new BookingRepository(ctx);

        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        Booking active1 = EntityProvider.CreateBooking(eventId, userId);
        Booking active2 = EntityProvider.CreateBooking(eventId, userId);
        Booking cancelled = EntityProvider.CreateBooking(eventId, userId);

        cancelled.Cancel();

        ctx.Bookings.AddRange(active1, active2, cancelled);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        int activeCount = await counter.CountAsync(
            b => b.UserId == userId && b.Status == BookingStatus.Pending,
            TestContext.Current.CancellationToken);

        // Assert
        activeCount.Should().Be(2);
    }

    [Fact]
    public async Task Confirm_PersistsStatusChange()
    {
        // Arrange
        await using BookingsDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        Guid eventId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Booking booking = EntityProvider.CreateBooking(eventId, userId);

        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Booking tracked = (await uow.Bookings.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken))!;

        // Act
        tracked.Confirm(DateTime.UtcNow);

        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using BookingsDbContext verifyCtx = Fixture.CreateContext();

        Booking? stored = await verifyCtx.Bookings.FindAsync([booking.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.Status.Should().Be(BookingStatus.Confirmed);
        stored.ProcessedAt.Should().NotBeNull();
    }
}
