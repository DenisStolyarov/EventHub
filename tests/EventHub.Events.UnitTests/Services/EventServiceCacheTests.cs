using EventHub.Events.Application.Abstractions.Caching;
using EventHub.Events.Application.Abstractions.Persistence;
using EventHub.Events.Application.Abstractions.Persistence.Repositories;
using EventHub.Events.Application.Dtos.Events;
using EventHub.Events.Application.IntegrationEvents;
using EventHub.Events.Application.Options;
using EventHub.Events.Application.Services;
using EventHub.Events.Domain.Entities;
using EventHub.Events.Domain.ValueObjects;
using EventHub.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

using static EventHub.Events.UnitTests.TestData.TestDateTime;

namespace EventHub.Events.UnitTests.Services;

public sealed class EventServiceCacheTests
{
    private const string DatabaseCall = "database";
    private const string CacheCall = "cache";

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly EventService _eventService;

    public EventServiceCacheTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<ICacheService>();

        _unitOfWorkMock.SetupGet(u => u.Events).Returns(_eventRepositoryMock.Object);

        CacheOptions cacheOptions = new() { EventTtlSeconds = 600, TopTtlSeconds = 60 };

        _eventService = new EventService(_unitOfWorkMock.Object, _cacheMock.Object, Options.Create(cacheOptions));
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedEventAndDoesNotQueryRepository()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        EventDto cached = CreateEventDto(id);

        _cacheMock
            .Setup(c => c.GetAsync<EventDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        EventDto result = await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(cached);

        _eventRepositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<EventDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_FetchesFromRepositoryAndPopulatesCache()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Event @event = CreateEvent(id);
        EventDto expected = @event.ToDto();

        _cacheMock
            .Setup(c => c.GetAsync<EventDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDto?)null);

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        EventDto result = await _eventService.GetByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<EventDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopAsync_CacheHit_ReturnsCachedEventsAndDoesNotQueryRepository()
    {
        // Arrange
        List<EventDto> cached = [CreateEventDto(Guid.NewGuid()), CreateEventDto(Guid.NewGuid())];

        _cacheMock
            .Setup(c => c.GetAsync<List<EventDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        IReadOnlyCollection<EventDto> result = await _eventService.GetTopAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(cached);

        _eventRepositoryMock.Verify(
            r => r.GetTopBySalesPercentageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopAsync_CacheMiss_FetchesFromRepositoryAndPopulatesCache()
    {
        // Arrange
        List<Event> events = [CreateEvent(Guid.NewGuid()), CreateEvent(Guid.NewGuid())];
        IReadOnlyCollection<EventDto> expected = [.. events.ToDto()];

        _cacheMock
            .Setup(c => c.GetAsync<List<EventDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EventDto>?)null);

        _eventRepositoryMock
            .Setup(r => r.GetTopBySalesPercentageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        IReadOnlyCollection<EventDto> result = await _eventService.GetTopAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<EventDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EventExists_SavesToDatabaseThenInvalidatesCache()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Event @event = CreateEvent(id);
        List<string> callOrder = [];

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(DatabaseCall));

        _cacheMock
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(CacheCall));

        UpdateEventDto dto = new()
        {
            Title = "Updated title",
            Description = "Updated description",
            StartAt = UtcDate(2026, 7, 1, 12),
            EndAt = UtcDate(2026, 7, 1, 15)
        };

        // Act
        await _eventService.UpdateAsync(id, dto, TestContext.Current.CancellationToken);

        // Assert
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        callOrder.Should().Equal(DatabaseCall, CacheCall);
    }

    [Fact]
    public async Task DeleteAsync_EventExists_InvalidatesCache()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Event @event = CreateEvent(id);

        _unitOfWorkMock
            .SetupGet(u => u.Outbox)
            .Returns(Mock.Of<IOutboxRepository>());

        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        await _eventService.DeleteAsync(id, TestContext.Current.CancellationToken);

        // Assert
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BookingCreatedHandler_SeatReserved_InvalidatesEventCache()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Event @event = CreateEvent(eventId);

        _unitOfWorkMock.SetupGet(u => u.Outbox).Returns(Mock.Of<IOutboxRepository>());
        _unitOfWorkMock.SetupGet(u => u.Inbox).Returns(Mock.Of<IInboxRepository>());
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        BookingCreatedHandler handler = new(_unitOfWorkMock.Object, _cacheMock.Object, NullLogger<BookingCreatedHandler>.Instance);
        BookingCreated message = new(Guid.NewGuid(), Guid.NewGuid(), eventId);

        // Act
        await handler.HandleAsync(message, TestContext.Current.CancellationToken);

        // Assert
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BookingCancelledHandler_SeatReleased_InvalidatesEventCache()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Event @event = CreateEvent(eventId);

        @event.TryReserveSeats();

        _unitOfWorkMock.SetupGet(u => u.Inbox).Returns(Mock.Of<IInboxRepository>());
        _eventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        BookingCancelledHandler handler = new(_unitOfWorkMock.Object, _cacheMock.Object, NullLogger<BookingCancelledHandler>.Instance);
        BookingCancelled message = new(Guid.NewGuid(), eventId);

        // Act
        await handler.HandleAsync(message, TestContext.Current.CancellationToken);

        // Assert
        _cacheMock.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Event CreateEvent(Guid id) =>
        new(id, "Event title", "Event description", 100, new Period(UtcDate(2026, 6, 1, 10).UtcDateTime, UtcDate(2026, 6, 1, 12).UtcDateTime));

    private static EventDto CreateEventDto(Guid id) => new()
    {
        Id = id,
        Title = "Event title",
        Description = "Event description",
        TotalSeats = 100,
        AvailableSeats = 100,
        StartAt = UtcDate(2026, 6, 1, 10),
        EndAt = UtcDate(2026, 6, 1, 12)
    };
}
