using EventHub.Application.Abstractions.Identity;
using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Abstractions.Services;
using EventHub.Application.Dtos.Bookings;
using EventHub.Application.Dtos.Events;
using EventHub.Application.Exceptions;
using EventHub.Application.Services;
using EventHub.Domain.Abstractions;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Services;
using EventHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Linq.Expressions;

using static EventHub.UnitTests.TestData.TestDateTime;

namespace EventHub.UnitTests.Services;

public sealed class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope;
    private readonly AppDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IBookingCounter> _bookingCounterMock;
    private readonly FakeTimeProvider _timeProvider = new(UtcDate(2026, 1, 1, 10));

    public BookingServiceTests()
    {
        string dbName = Guid.NewGuid().ToString();

        _currentUserMock = new();
        _currentUserMock
            .SetupGet(c => c.Id)
            .Returns(Guid.NewGuid());

        _bookingCounterMock = new();
        _bookingCounterMock
            .Setup(c => c.CountAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ServiceCollection services = new();

        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService>(sp =>
        {
            IUnitOfWork unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            BookingManager bookingManager = new(_bookingCounterMock.Object, _timeProvider);

            return new BookingService(bookingManager, _currentUserMock.Object, unitOfWork);
        });

        _serviceProvider = services.BuildServiceProvider();
        _serviceScope = _serviceProvider.CreateScope();

        _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _bookingService = _serviceScope.ServiceProvider.GetRequiredService<IBookingService>();
        _eventService = _serviceScope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceScope.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task CreateBookingAsync_EventExists_ReturnsPendingBookingInfo()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 1);

        // Act
        BookingInfo result = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);
        result.EventId.Should().Be(@event.Id);
        result.Status.Should().Be(BookingStatus.Pending);

        Event? storedEvent = await _dbContext.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent.AvailableSeats.Should().Be(0);

        Booking? storedBooking = await _dbContext.Bookings.FindAsync([result.Id], TestContext.Current.CancellationToken);

        storedBooking.Should().NotBeNull();
        storedBooking.EventId.Should().Be(@event.Id);
        storedBooking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_SameEventBookedMultipleTimes_ReturnsUniqueIds()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 2);

        // Act
        BookingInfo first = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
        BookingInfo second = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        first.Id.Should().NotBe(second.Id);
        first.Id.Should().NotBe(Guid.Empty);
        second.Id.Should().NotBe(Guid.Empty);
        first.EventId.Should().Be(@event.Id);
        second.EventId.Should().Be(@event.Id);
        first.Status.Should().Be(BookingStatus.Pending);
        second.Status.Should().Be(BookingStatus.Pending);

        Event? storedEvent = await _dbContext.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(0);

        List<Booking> storedBookings = await _dbContext.Bookings.ToListAsync(TestContext.Current.CancellationToken);

        storedBookings.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateBookingAsync_NotEnoughSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 1);

        // Act
        BookingInfo first = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
        Func<Task> second = () => _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        first.EventId.Should().Be(@event.Id);
        first.Status.Should().Be(BookingStatus.Pending);

        Event? storedEvent = await _dbContext.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(0);

        await second.Should().ThrowAsync<NoAvailableSeatsException>();

        List<Booking> storedBookings = await _dbContext.Bookings.ToListAsync(TestContext.Current.CancellationToken);

        storedBookings.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingExists_ReturnsBookingInfo()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = created.Id,
            EventId = @event.Id,
            Status = BookingStatus.Pending,
        });
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingConfirmed_ReturnsUpdatedStatus()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
        Booking? booking = await _dbContext.Bookings.FindAsync([created.Id], TestContext.Current.CancellationToken);

        booking.Should().NotBeNull();
        booking.Confirm(_timeProvider.GetUtcNow().UtcDateTime);

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingRejected_ReturnsUpdatedStatus()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
        Booking? booking = await _dbContext.Bookings.FindAsync([created.Id], TestContext.Current.CancellationToken);

        booking.Should().NotBeNull();
        booking.Reject(_timeProvider.GetUtcNow().UtcDateTime);

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    public async Task CreateBookingAsync_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(eventId) && e.EntityName == nameof(Event));

        List<Booking> storedBookings = await _dbContext.Bookings.ToListAsync(TestContext.Current.CancellationToken);

        storedBookings.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBookingAsync_UnauthenticatedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();

        _currentUserMock.SetupGet(c => c.Id).Returns((Guid?)null);

        // Act
        Func<Task> act = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _bookingService.GetBookingByIdAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(bookingId) && e.EntityName == nameof(Booking));
    }

    [Fact]
    public async Task GetBookingByIdAsync_NotOwner_ThrowsForbiddenException()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        _currentUserMock.SetupGet(c => c.Id).Returns(Guid.NewGuid());

        // Act
        Func<Task> act = () => _bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_AdminCanAccessAnyBooking()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        _currentUserMock.SetupGet(c => c.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(true);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentOverbooking_ExactlyLimitSucceedsAndExcessThrows()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;

        EventDto @event = await CreateEventAsync(totalSeats);

        // Act
        Task<BookingInfo>[] tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
            }))
            .ToArray();

        await Task.WhenAll(tasks.Select(t => t.ContinueWith(_ => { }, TaskScheduler.Current)));

        List<BookingInfo> succeeded = tasks
            .Where(t => t.IsCompletedSuccessfully)
            .Select(t => t.Result)
            .ToList();

        List<Exception> exceptions = tasks
            .Where(t => t.IsFaulted)
            .Select(t => t.Exception!.InnerException!)
            .ToList();

        // Assert
        succeeded.Should().HaveCount(totalSeats);
        exceptions.Should().HaveCount(concurrentRequests - totalSeats);
        exceptions.Should().AllSatisfy(e => e.Should().BeOfType<NoAvailableSeatsException>());

        succeeded.Should().OnlyContain(b => b.EventId == @event.Id);
        succeeded.Should().OnlyContain(b => b.Status == BookingStatus.Pending);
        succeeded.Select(b => b.Id).Should().OnlyHaveUniqueItems();

        await using AsyncServiceScope assertScope = _serviceProvider.CreateAsyncScope();
        AppDbContext db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Event? storedEvent = await db.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent.AvailableSeats.Should().Be(0);

        List<Booking> storedBookings = await db.Bookings.ToListAsync(TestContext.Current.CancellationToken);

        storedBookings.Should().HaveCount(totalSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllReturnUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        EventDto @event = await CreateEventAsync(totalSeats);

        // Act
        IEnumerable<Task<BookingInfo>> tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => Task.Run(async () =>
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                IBookingService bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
            }));

        BookingInfo[] results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(totalSeats);
        results.Should().OnlyContain(b => b.EventId == @event.Id);
        results.Should().OnlyContain(b => b.Status == BookingStatus.Pending);
        results.Select(b => b.Id).Should().OnlyHaveUniqueItems();

        await using AsyncServiceScope assertScope = _serviceProvider.CreateAsyncScope();
        AppDbContext db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Event? storedEvent = await db.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(0);

        List<Booking> storedBookings = await db.Bookings.ToListAsync(TestContext.Current.CancellationToken);

        storedBookings.Should().HaveCount(totalSeats);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingExists_ReturnsCancelledBookingInfo()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 1);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        Booking? booking = await _dbContext.Bookings.FindAsync([created.Id], TestContext.Current.CancellationToken);

        booking.Should().NotBeNull();

        _currentUserMock.SetupGet(c => c.Id).Returns(booking.UserId);

        // Act
        BookingInfo result = await _bookingService.CancelBookingAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);

        Event? storedEvent = await _dbContext.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(bookingId) && e.EntityName == nameof(Booking));
    }

    [Fact]
    public async Task CancelBookingAsync_NotOwner_ThrowsForbiddenException()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        _currentUserMock.SetupGet(c => c.Id).Returns(Guid.NewGuid());

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsAnyBooking_ReturnsCancelledBookingInfo()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 1);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        _currentUserMock.SetupGet(c => c.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(true);

        // Act
        BookingInfo result = await _bookingService.CancelBookingAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);

        Event? storedEvent = await _dbContext.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent!.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task CancelBookingAsync_UnauthenticatedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        EventDto @event = await CreateEventAsync(totalSeats: 10);
        BookingInfo created = await _bookingService.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);

        _currentUserMock.SetupGet(c => c.Id).Returns((Guid?)null);

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(created.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    private async Task<EventDto> CreateEventAsync(int totalSeats = 100)
    {
        CreateEventDto dto = new()
        {
            Title = "Event title",
            Description = "Event description",
            TotalSeats = totalSeats,
            StartAt = UtcDate(2026, 6, 1, 10),
            EndAt = UtcDate(2026, 6, 1, 12)
        };

        return await _eventService.CreateAsync(dto, TestContext.Current.CancellationToken);
    }
}
