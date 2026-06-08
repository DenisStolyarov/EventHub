using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Enums;
using EventHub.Api.Infrastructure.Repositories;
using FluentAssertions;

namespace EventHub.Tests.Repositories;

public class InMemoryBookingRepositoryTests
{
    private readonly InMemoryBookingRepository _repository = new();

    [Fact]
    public void Add_ValidBooking_StoresIt()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        _repository.Add(booking);

        // Assert
        Booking? result = _repository.GetById(booking.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(booking.EventId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public void GetById_NonExisting_ReturnsNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        Booking? result = _repository.GetById(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPendingBookings_MixedStatuses_ReturnsOnlyPending()
    {
        // Arrange
        Booking pending1 = new(Guid.NewGuid(), Guid.NewGuid());
        Booking pending2 = new(Guid.NewGuid(), Guid.NewGuid());
        Booking confirmed = new(Guid.NewGuid(), Guid.NewGuid());
        confirmed.Confirm();

        _repository.Add(pending1);
        _repository.Add(pending2);
        _repository.Add(confirmed);

        // Act
        IEnumerable<Booking> result = _repository.GetPendingBookings();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.Pending));
    }

    [Fact]
    public void Update_ExistingBooking_UpdatesStatus()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        _repository.Add(booking);

        booking.Confirm();

        // Act
        _repository.Update(booking);

        // Assert
        Booking? result = _repository.GetById(booking.Id);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_NonExisting_ThrowsKeyNotFound()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Action act = () => _repository.Update(booking);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Delete_ExistingBooking_RemovesIt()
    {
        // Arrange
        Booking booking = new(Guid.NewGuid(), Guid.NewGuid());

        _repository.Add(booking);

        // Act
        _repository.Delete(booking.Id);

        // Assert
        _repository.GetById(booking.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExisting_ThrowsKeyNotFound()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        Action act = () => _repository.Delete(id);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }
}