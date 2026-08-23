using EventHub.Application.Abstractions.Persistence;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using EventHub.IntegrationTests.Abstractions;
using EventHub.IntegrationTests.Fixtures;
using EventHub.IntegrationTests.TestData;
using FluentAssertions;

namespace EventHub.IntegrationTests.Repositories;

public class BookingRepositoryTests(IntegrationTestFixture fixture) : RepositoryTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetByIdAsync_ReturnsBooking_WhenExists()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();
        Booking booking = EntityProvider.CreateBooking(@event.Id);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Booking? result = await uow.Bookings.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(@event.Id);
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
        Event @event = EntityProvider.CreateEvent();
        Booking booking = EntityProvider.CreateBooking(@event.Id);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        uow.Bookings.Add(booking);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Booking? stored = await verifyCtx.Bookings.FindAsync([booking.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.EventId.Should().Be(@event.Id);
        stored.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task Confirm_PersistsStatusChange()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();
        Booking booking = EntityProvider.CreateBooking(@event.Id);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.Add(booking);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Booking tracked = (await uow.Bookings.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken))!;

        // Act
        tracked.Confirm();

        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Booking? stored = await verifyCtx.Bookings.FindAsync([booking.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.Status.Should().Be(BookingStatus.Confirmed);
        stored.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_ReturnsOnlyPendingBookingIds()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent(totalSeats: 10);
        Booking pending1 = EntityProvider.CreateBooking(@event.Id);
        Booking pending2 = EntityProvider.CreateBooking(@event.Id);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.AddRange(pending1, pending2);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        ICollection<Guid> result = await uow.Bookings.GetPendingBookingIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain([pending1.Id, pending2.Id]);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_ExcludesConfirmedAndRejected()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent(totalSeats: 10);
        Booking pending = EntityProvider.CreateBooking(@event.Id);
        Booking confirmed = EntityProvider.CreateBooking(@event.Id);
        Booking rejected = EntityProvider.CreateBooking(@event.Id);

        confirmed.Confirm();
        rejected.Reject();

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.AddRange(pending, confirmed, rejected);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        ICollection<Guid> result = await uow.Bookings.GetPendingBookingIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle(id => id == pending.Id);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_ReturnsEmpty_WhenNoPendingBookings()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent(totalSeats: 10);
        Booking confirmed = EntityProvider.CreateBooking(@event.Id);

        confirmed.Confirm();

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.Add(confirmed);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        ICollection<Guid> result = await uow.Bookings.GetPendingBookingIdsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }
}
