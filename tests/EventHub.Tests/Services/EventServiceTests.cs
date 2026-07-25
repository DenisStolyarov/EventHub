using EventHub.Api.Application.Constants;
using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Interfaces;
using EventHub.Api.Application.Services;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Exceptions;
using EventHub.Api.Domain.ValueObjects;
using EventHub.Api.Infrastructure.DataAccess;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using static EventHub.Tests.TestUtilities.TestDateTime;

namespace EventHub.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope;
    private readonly AppDbContext _dbContext;
    private readonly IEventService _eventService;

    public EventServiceTests()
    {
        string dbName = Guid.NewGuid().ToString();

        ServiceCollection services = new();

        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
        _serviceScope = _serviceProvider.CreateScope();

        _dbContext = _serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _eventService = _serviceScope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_AddsEventAndReturnsDto()
    {
        // Arrange
        DateTimeOffset startAt = UtcDate(2026, 6, 1, 10);
        DateTimeOffset endAt = UtcDate(2026, 6, 1, 12);
        CreateEventDto dto = new()
        {
            Title = "Community Meetup",
            Description = "Local tech talks",
            TotalSeats = 100,
            StartAt = startAt,
            EndAt = endAt
        };

        // Act
        EventDto result = await _eventService.CreateAsync(dto);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(new
        {
            dto.Title,
            dto.Description,
            TotalSeats = 100,
            AvailableSeats = 100,
            StartAt = startAt,
            EndAt = endAt
        });

        Event? storedEvent = await _dbContext.Events.FindAsync([result.Id], TestContext.Current.CancellationToken);
        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be(dto.Title);
        storedEvent.Description.Should().Be(dto.Description);
        storedEvent.TotalSeats.Should().Be(100);
        storedEvent.AvailableSeats.Should().Be(100);
        storedEvent.StartAt.Should().Be(startAt.UtcDateTime);
        storedEvent.EndAt.Should().Be(endAt.UtcDateTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_InvalidTitle_ThrowsValidationException(string? title)
    {
        // Arrange
        CreateEventDto dto = new()
        {
            Title = title!,
            Description = "Invalid event",
            TotalSeats = 100,
            StartAt = UtcDate(2026, 6, 1, 10),
            EndAt = UtcDate(2026, 6, 1, 11)
        };

        // Act
        Func<Task> act = () => _eventService.CreateAsync(dto);

        // Assert
        (await act.Should()
            .ThrowAsync<ValidationException>())
            .Where(e => e.Errors.ContainsKey(nameof(Event.Title)));

        List<Event> storedEvents = await _dbContext.Events.ToListAsync(TestContext.Current.CancellationToken);

        storedEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_InvalidTotalSeats_ThrowsValidationException(int totalSeats)
    {
        // Arrange
        CreateEventDto dto = new()
        {
            Title = "Invalid seats",
            Description = null,
            TotalSeats = totalSeats,
            StartAt = UtcDate(2026, 6, 1, 10),
            EndAt = UtcDate(2026, 6, 1, 11)
        };

        // Act
        Func<Task> act = () => _eventService.CreateAsync(dto);

        // Assert
        (await act.Should()
            .ThrowAsync<ValidationException>())
            .Where(e => e.Errors.ContainsKey(nameof(Event.TotalSeats)));

        List<Event> storedEvents = await _dbContext.Events.ToListAsync(TestContext.Current.CancellationToken);

        storedEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(10, 9)]
    [InlineData(10, 10)]
    public async Task CreateAsync_InvalidDateRange_ThrowsDomainException(int startHour, int endHour)
    {
        // Arrange
        CreateEventDto dto = new()
        {
            Title = "Invalid dates",
            Description = null,
            TotalSeats = 100,
            StartAt = UtcDate(2026, 6, 1, startHour),
            EndAt = UtcDate(2026, 6, 1, endHour)
        };

        // Act
        Func<Task> act = () => _eventService.CreateAsync(dto);

        // Assert
        (await act.Should()
            .ThrowAsync<DomainException>())
            .Where(e => e.Property == nameof(Period.EndAt));

        List<Event> storedEvents = await _dbContext.Events.ToListAsync(TestContext.Current.CancellationToken);

        storedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_EventExists_UpdatesEventAndReturnsDto()
    {
        // Arrange
        DateTimeOffset startAt = UtcDate(2026, 7, 1, 12);
        DateTimeOffset endAt = UtcDate(2026, 7, 1, 15);
        UpdateEventDto dto = new()
        {
            Title = "Updated title",
            Description = "Updated description",
            StartAt = startAt,
            EndAt = endAt
        };

        EventDto existing = await CreateEventAsync();

        // Act
        EventDto result = await _eventService.UpdateAsync(existing.Id, dto);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = existing.Id,
            dto.Title,
            dto.Description,
            TotalSeats = existing.TotalSeats,
            AvailableSeats = existing.AvailableSeats,
            StartAt = startAt,
            EndAt = endAt
        });

        Event? storedEvent = await _dbContext.Events.FindAsync([existing.Id], TestContext.Current.CancellationToken);
        storedEvent.Should().NotBeNull();
        storedEvent!.Title.Should().Be(dto.Title);
        storedEvent.Description.Should().Be(dto.Description);
        storedEvent.StartAt.Should().Be(startAt.UtcDateTime);
        storedEvent.EndAt.Should().Be(endAt.UtcDateTime);
    }

    [Fact]
    public async Task UpdateAsync_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        UpdateEventDto dto = new()
        {
            Title = "Updated",
            Description = "Updated description",
            StartAt = UtcDate(2026, 6, 1, 10),
            EndAt = UtcDate(2026, 6, 1, 11)
        };

        Guid id = Guid.NewGuid();

        // Act
        Func<Task> act = () => _eventService.UpdateAsync(id, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(10, 9)]
    [InlineData(10, 10)]
    public async Task UpdateAsync_InvalidDateRange_ThrowsDomainException(int startHour, int endHour)
    {
        // Arrange
        EventDto existing = await CreateEventAsync();
        UpdateEventDto dto = new()
        {
            Title = "Invalid dates",
            Description = null,
            StartAt = UtcDate(2026, 6, 1, startHour),
            EndAt = UtcDate(2026, 6, 1, endHour)
        };

        // Act
        Func<Task> act = () => _eventService.UpdateAsync(existing.Id, dto);

        // Assert
        (await act.Should()
            .ThrowAsync<DomainException>())
            .Where(e => e.Property == nameof(Period.EndAt));

        Event? storedEvent = await _dbContext.Events.FindAsync([existing.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent.Title.Should().Be(existing.Title);
    }

    [Fact]
    public async Task GetByIdAsync_EventExists_ReturnsEvent()
    {
        // Arrange
        EventDto existing = await CreateEventAsync(startAt: UtcDate(2026, 6, 3, 10), endAt: UtcDate(2026, 6, 3, 11));

        // Act
        EventDto result = await _eventService.GetByIdAsync(existing.Id);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            existing.Id,
            existing.Title,
            existing.Description,
            existing.TotalSeats,
            existing.AvailableSeats,
            existing.StartAt,
            existing.EndAt,
        });
    }

    [Fact]
    public async Task GetByIdAsync_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        Func<Task> act = () => _eventService.GetByIdAsync(id);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(id) && e.EntityName == nameof(Event));
    }

    [Fact]
    public async Task GetAllAsync_EventsExist_ReturnsDataAndPaginationMetadata()
    {
        // Arrange
        await CreateEventAsync(title: "First event");
        await CreateEventAsync(title: "Second event");

        EventDto third = await CreateEventAsync(title: "Third event");
        EventDto fourth = await CreateEventAsync(title: "Fourth event");

        await CreateEventAsync(title: "Fifth event");

        GetEventsDto dto = new() { Page = 2, PageSize = 2 };

        // Act
        PaginatedResult<EventDto> result = await _eventService.GetAllAsync(dto);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            PageNumber = 2,
            PageSize = 2,
            TotalRecords = 5,
            TotalPages = 3,
            ItemsOnPage = 2
        });

        result.Data.Select(e => e.Id)
            .Should()
            .Equal(third.Id, fourth.Id);
    }

    [Theory]
    [InlineData("Meetup")]
    public async Task GetAllAsync_TitleFilterProvided_FindsEvents(string? title)
    {
        // Arrange
        EventDto meetup = await CreateEventAsync(title: "Community Meetup");

        await CreateEventAsync(title: "Architecture Workshop");

        GetEventsDto dto = new() { Title = title };

        // Act
        PaginatedResult<EventDto> result = await _eventService.GetAllAsync(dto);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.Data.Single().Id.Should().Be(meetup.Id);
    }

    [Fact]
    public async Task GetAllAsync_DateFiltersProvided_ReturnsEventsWithinDateRange()
    {
        // Arrange
        EventDto early = await CreateEventAsync(
            title: "Early event",
            startAt: UtcDate(2026, 6, 1, 10),
            endAt: UtcDate(2026, 6, 1, 12));

        await CreateEventAsync(
            title: "Late event",
            startAt: UtcDate(2026, 6, 15, 10),
            endAt: UtcDate(2026, 6, 15, 12));

        GetEventsDto dto = new()
        {
            From = UtcDate(2026, 6, 1, 0),
            To = UtcDate(2026, 6, 10, 23)
        };

        // Act
        PaginatedResult<EventDto> result = await _eventService.GetAllAsync(dto);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.Data.Single().Id.Should().Be(early.Id);
    }

    [Fact]
    public async Task GetAllAsync_FromAfterTo_ThrowsValidationException()
    {
        // Arrange
        GetEventsDto dto = new()
        {
            From = UtcDate(2026, 7, 2, 10),
            To = UtcDate(2026, 7, 1, 10)
        };

        // Act
        Func<Task> act = () => _eventService.GetAllAsync(dto);

        // Assert
        (await act.Should()
            .ThrowAsync<ValidationException>())
            .Where(e => e.Errors.ContainsKey(nameof(GetEventsDto.From)));
    }

    [Theory]
    [InlineData(-10, 25, 1, 25)]
    [InlineData(0, 25, 1, 25)]
    [InlineData(2, 0, 2, 1)]
    [InlineData(2, -10, 2, 1)]
    [InlineData(1, Pagination.MaxPageSize + 1, 1, Pagination.MaxPageSize)]
    public async Task GetAllAsync_PaginationValuesProvided_NormalizesPagination(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        // Arrange
        await CreateEventAsync(title: "Event 1");
        await CreateEventAsync(title: "Event 2");

        GetEventsDto dto = new() { Page = page, PageSize = pageSize };

        // Act
        PaginatedResult<EventDto> result = await _eventService.GetAllAsync(dto);

        // Assert
        result.PageNumber.Should().Be(expectedPage);
        result.PageSize.Should().Be(expectedPageSize);
    }

    [Fact]
    public async Task GetAllAsync_CombinedFilterProvided_ReturnsMatchingEvents()
    {
        // Arrange
        await CreateEventAsync(
            title: "Other conference",
            startAt: UtcDate(2026, 8, 1, 9),
            endAt: UtcDate(2026, 8, 1, 18));

        EventDto target = await CreateEventAsync(
            title: "Target conference",
            startAt: UtcDate(2026, 8, 10, 9),
            endAt: UtcDate(2026, 8, 10, 18));

        await CreateEventAsync(
            title: "Another workshop",
            startAt: UtcDate(2026, 8, 10, 9),
            endAt: UtcDate(2026, 8, 10, 18));

        // Act
        PaginatedResult<EventDto> result = await _eventService.GetAllAsync(new GetEventsDto
        {
            Title = "Target",
            From = UtcDate(2026, 8, 1, 0),
            To = UtcDate(2026, 8, 31, 23),
            Page = 1,
            PageSize = 10
        });

        // Assert
        result.TotalRecords.Should().Be(1);
        result.Data.Single().Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task DeleteAsync_EventExists_DeletesEvent()
    {
        // Arrange
        EventDto existing = await CreateEventAsync();

        // Act
        await _eventService.DeleteAsync(existing.Id);

        // Assert
        Event? storedEvent = await _dbContext.Events.FindAsync([existing.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        Func<Task> act = () => _eventService.DeleteAsync(id);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .Where(e => e.EntityId.Equals(id) && e.EntityName == nameof(Event));
    }

    private async Task<EventDto> CreateEventAsync(
        string? title = null,
        int totalSeats = 100,
        DateTimeOffset? startAt = null,
        DateTimeOffset? endAt = null)
    {
        DateTimeOffset start = startAt ?? UtcDate(2026, 6, 1, 10);
        DateTimeOffset end = endAt ?? UtcDate(2026, 6, 1, 12);

        CreateEventDto dto = new()
        {
            Title = title ?? "Event title",
            Description = "Event description",
            TotalSeats = totalSeats,
            StartAt = start,
            EndAt = end
        };

        return await _eventService.CreateAsync(dto);
    }
}