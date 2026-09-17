using System.Linq.Expressions;
using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Application.Abstractions.Persistence.Repositories;
using EventHub.Bookings.Application.Dtos.Bookings;
using EventHub.Bookings.Application.Exceptions;
using EventHub.Bookings.Application.Services;
using EventHub.Bookings.Domain.Abstractions;
using EventHub.Bookings.Domain.Entities;
using EventHub.Bookings.Domain.Enums;
using EventHub.Bookings.Domain.Exceptions;
using EventHub.Bookings.Domain.Services;
using EventHub.Shared.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;

using static EventHub.Bookings.UnitTests.TestData.TestDateTime;

namespace EventHub.Bookings.UnitTests.Services;

public sealed class BookingServiceTests
{
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IBookingCounter> _bookingCounterMock;
    private readonly Mock<IBookingRepository> _bookingsMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly FakeTimeProvider _timeProvider = new(UtcDate(2026, 1, 1, 10));
    private readonly Guid _userId = Guid.NewGuid();
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _currentUserMock = new();
        _currentUserMock
            .SetupGet(c => c.Id)
            .Returns(_userId);
        _currentUserMock
            .Setup(c => c.IsInRole(It.IsAny<string>()))
            .Returns(false);

        _bookingCounterMock = new();
        _bookingCounterMock
            .Setup(c => c.CountAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _bookingsMock = new();

        _unitOfWorkMock = new();
        _unitOfWorkMock
            .SetupGet(u => u.Bookings)
            .Returns(_bookingsMock.Object);

        BookingManager bookingManager = new(_bookingCounterMock.Object, _timeProvider);

        _bookingService = new BookingService(bookingManager, _currentUserMock.Object, _unitOfWorkMock.Object);
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

        _bookingsMock.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_ReturnsPendingBookingInfoAndPersists()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();

        // Act
        BookingInfo result = await _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be(BookingStatus.Pending);
        result.CreatedAt.Should().Be(UtcDateTime(2026, 1, 1, 10));

        _bookingsMock.Verify(
            b => b.Add(It.Is<Booking>(booking =>
                booking.Id == result.Id
                && booking.EventId == eventId
                && booking.UserId == _userId
                && booking.Status == BookingStatus.Pending)),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_LimitExceeded_ThrowsAndDoesNotPersist()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();

        _bookingCounterMock
            .Setup(c => c.CountAsync(
                It.IsAny<Expression<Func<Booking, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookingManager.MaxActiveBookingsAllowed);

        // Act
        Func<Task> act = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<MaxActiveBookingsExceededException>();

        _bookingsMock.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_EmptyEventId_ThrowsValidationException()
    {
        // Arrange
        Guid eventId = Guid.Empty;

        // Act
        Func<Task> act = () => _bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        _bookingsMock.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingExistsAndUserIsOwner_ReturnsBookingInfo()
    {
        // Arrange
        Booking booking = CreateBooking(userId: _userId);

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(booking.EventId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();

        _bookingsMock
            .Setup(b => b.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

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
        Booking booking = CreateBooking(userId: Guid.NewGuid());

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_AdminCanAccessAnyBooking()
    {
        // Arrange
        Booking booking = CreateBooking(userId: Guid.NewGuid());

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserMock
            .Setup(c => c.IsInRole(It.IsAny<string>()))
            .Returns(true);

        // Act
        BookingInfo result = await _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_UnauthenticatedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        Booking booking = CreateBooking(userId: Guid.NewGuid());

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserMock.SetupGet(c => c.Id).Returns((Guid?)null);

        // Act
        Func<Task> act = () => _bookingService.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task CancelBookingAsync_BookingExistsAndUserIsOwner_ReturnsCancelledBookingInfo()
    {
        // Arrange
        Booking booking = CreateBooking(userId: _userId);

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        BookingInfo result = await _bookingService.CancelBookingAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);
        result.Id.Should().Be(booking.Id);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid bookingId = Guid.NewGuid();

        _bookingsMock
            .Setup(b => b.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(bookingId) && e.EntityName == nameof(Booking));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_NotOwner_ThrowsForbiddenException()
    {
        // Arrange
        Booking booking = CreateBooking(userId: Guid.NewGuid());

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();

        booking.Status.Should().Be(BookingStatus.Pending);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsAnyBooking_ReturnsCancelledBookingInfo()
    {
        // Arrange
        Booking booking = CreateBooking(userId: Guid.NewGuid());

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserMock
            .Setup(c => c.IsInRole(It.IsAny<string>()))
            .Returns(true);

        // Act
        BookingInfo result = await _bookingService.CancelBookingAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(BookingStatus.Cancelled);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ThrowsDomainException()
    {
        // Arrange
        Booking booking = CreateBooking(userId: _userId);
        booking.Cancel();

        _bookingsMock
            .Setup(b => b.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => _bookingService.CancelBookingAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<DomainException>();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Booking CreateBooking(Guid userId) =>
        new(Guid.CreateVersion7(), Guid.NewGuid(), userId, UtcDateTime(2026, 5, 1, 10));
}
