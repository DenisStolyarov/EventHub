using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Exceptions;
using EventHub.Api.Domain.ValueObjects;
using FluentAssertions;

using static EventHub.Tests.TestUtilities.TestDateTime;

namespace EventHub.Tests.Entities;

public class EventTests
{
    [Fact]
    public void Create_ValidParameters_SetsAllFieldsAndEqualAvailableSeats()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Period period = new(UtcDateTime(2026, 6, 1, 10), UtcDateTime(2026, 6, 1, 12));

        // Act
        Event @event = new(id, "Title", "Description", 50, period);

        // Assert
        @event.Id.Should().Be(id);
        @event.Title.Should().Be("Title");
        @event.Description.Should().Be("Description");
        @event.StartAt.Should().Be(period.StartAt);
        @event.EndAt.Should().Be(period.EndAt);
        @event.TotalSeats.Should().Be(50);
        @event.AvailableSeats.Should().Be(50);
    }

    [Fact]
    public void Create_TitleWithSurroundingWhitespace_TrimsTitle()
    {
        // Arrange
        Period period = new(UtcDateTime(2026, 6, 1, 10), UtcDateTime(2026, 6, 1, 12));

        // Act
        Event @event = new(Guid.NewGuid(), "  Spaced Title  ", null, 50, period);

        // Assert
        @event.Title.Should().Be("Spaced Title");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveTotalSeats_ThrowsValidationException(int totalSeats)
    {
        // Arrange
        Period period = new(UtcDateTime(2026, 6, 1, 10), UtcDateTime(2026, 6, 1, 12));

        // Act
        Action act = () => new Event(Guid.NewGuid(), "Title", "Description", totalSeats, period);

        // Assert
        act.Should()
            .Throw<ValidationException>()
            .Where(e => e.Errors.ContainsKey(nameof(Event.TotalSeats)));
    }

    [Fact]
    public void TryReserveSeats_EnoughSeats_DecreasesAvailableAndReturnsTrue()
    {
        // Arrange
        Event @event = CreateEvent(10);

        // Act
        bool result = @event.TryReserveSeats(3);

        // Assert
        result.Should().BeTrue();
        @event.AvailableSeats.Should().Be(7);
        @event.TotalSeats.Should().Be(10);
    }

    [Fact]
    public void TryReserveSeats_AllSeats_DecreasesAvailableToZeroAndReturnsTrue()
    {
        // Arrange
        Event @event = CreateEvent(10);

        // Act
        bool result = @event.TryReserveSeats(10);

        // Assert
        result.Should().BeTrue();
        @event.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_NotEnoughSeats_ReturnsFalseAndKeepsAvailable()
    {
        // Arrange
        Event @event = CreateEvent(5);

        // Act
        bool result = @event.TryReserveSeats(6);

        // Assert
        result.Should().BeFalse();
        @event.AvailableSeats.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryReserveSeats_NonPositiveCount_ReturnsFalseAndKeepsAvailable(int count)
    {
        // Arrange
        Event @event = CreateEvent(5);

        // Act
        bool result = @event.TryReserveSeats(count);

        // Assert
        result.Should().BeFalse();
        @event.AvailableSeats.Should().Be(5);
    }

    [Fact]
    public void TryReserveSeats_DefaultCount_ReservesSingleSeat()
    {
        // Arrange
        Event @event = CreateEvent(5);

        // Act
        bool result = @event.TryReserveSeats();

        // Assert
        result.Should().BeTrue();
        @event.AvailableSeats.Should().Be(4);
    }

    [Fact]
    public void TryReserveSeats_CalledMultipleTimes_RejectsWhenAvailableExhausted()
    {
        // Arrange
        Event @event = CreateEvent(10);

        // Act
        bool first = @event.TryReserveSeats(8);
        bool second = @event.TryReserveSeats(5);

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        @event.AvailableSeats.Should().Be(2);
        @event.AvailableSeats.Should().BeLessThanOrEqualTo(@event.TotalSeats);
    }

    [Fact]
    public void ReleaseSeats_ValidCount_IncreasesAvailable()
    {
        // Arrange
        Event @event = CreateEvent(10);
        @event.TryReserveSeats(4);

        // Act
        @event.ReleaseSeats(3);

        // Assert
        @event.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public void ReleaseSeats_ExceedsTotalSeats_ThrowsDomainException()
    {
        // Arrange
        Event @event = CreateEvent(10);
        @event.TryReserveSeats(2);

        // Act
        Action act = () => @event.ReleaseSeats(10);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Where(e => e.Property == nameof(Event.AvailableSeats));

        @event.AvailableSeats.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseSeats_NonPositiveCount_ThrowsDomainException(int count)
    {
        // Arrange
        Event @event = CreateEvent(10);

        // Act
        Action act = () => @event.ReleaseSeats(count);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Where(e => e.Property == nameof(Event.AvailableSeats));

        @event.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void ReleaseSeats_DefaultCount_ReleasesSingleSeat()
    {
        // Arrange
        Event @event = CreateEvent(10);
        @event.TryReserveSeats(3);

        // Act
        @event.ReleaseSeats();

        // Assert
        @event.AvailableSeats.Should().Be(8);
    }

    [Fact]
    public void ReleaseSeats_ExactlyToTotal_SucceedsAndRestoresAllSeats()
    {
        // Arrange
        Event @event = CreateEvent(10);
        @event.TryReserveSeats(4);

        // Act
        @event.ReleaseSeats(4);

        // Assert
        @event.AvailableSeats.Should().Be(10);
        @event.AvailableSeats.Should().Be(@event.TotalSeats);
    }

    [Fact]
    public void Update_ValidParameters_UpdatesMutableFieldsAndPreservesSeats()
    {
        // Arrange
        Event @event = CreateEvent(50);
        @event.TryReserveSeats(20);

        Period newPeriod = new(UtcDateTime(2026, 7, 1, 9), UtcDateTime(2026, 7, 1, 18));

        // Act
        @event.Update("New title", "New description", newPeriod);

        // Assert
        @event.Title.Should().Be("New title");
        @event.Description.Should().Be("New description");
        @event.StartAt.Should().Be(newPeriod.StartAt);
        @event.EndAt.Should().Be(newPeriod.EndAt);
        @event.TotalSeats.Should().Be(50);
        @event.AvailableSeats.Should().Be(30);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Update_InvalidTitle_ThrowsValidationException(string? title)
    {
        // Arrange
        Event @event = CreateEvent(50);
        Period newPeriod = new(UtcDateTime(2026, 7, 1, 9), UtcDateTime(2026, 7, 1, 18));

        // Act
        Action act = () => @event.Update(title!, "New description", newPeriod);

        // Assert
        act.Should()
            .Throw<ValidationException>()
            .Where(e => e.Errors.ContainsKey(nameof(Event.Title)));

        @event.Title.Should().Be("Event title");
    }

    private static Event CreateEvent(int totalSeats)
    {
        Period period = new(UtcDateTime(2026, 6, 1, 10), UtcDateTime(2026, 6, 1, 12));

        return new Event(Guid.NewGuid(), "Event title", "Event description", totalSeats, period);
    }
}
