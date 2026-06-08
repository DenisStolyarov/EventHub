using EventHub.Api.Application.Constants;
using EventHub.Api.Application.Dto;
using EventHub.Api.Application.Dto.Events;
using EventHub.Api.Application.Exceptions;
using EventHub.Api.Application.Services;
using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Exceptions;
using EventHub.Api.Domain.Filters;
using EventHub.Api.Domain.Interfaces;
using EventHub.Api.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace EventHub.Tests;

public class EventServiceTests
{
    private readonly EventService _service;
    private readonly Mock<IEventRepository> _repository;

    public EventServiceTests()
    {
        _repository = new();
        _service = new(_repository.Object);
    }

    [Fact]
    public void Create_ValidDto_AddsEventAndReturnsDto()
    {
        // Arrange
        DateTimeOffset startAt = UtcDate(2026, 6, 1, 10);
        DateTimeOffset endAt = UtcDate(2026, 6, 1, 12);
        CreateEventDto dto = new()
        {
            Title = "Community Meetup",
            Description = "Local tech talks",
            StartAt = startAt,
            EndAt = endAt
        };

        // Act
        EventDto result = _service.Create(dto);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Should().BeEquivalentTo(new
        {
            dto.Title,
            dto.Description,
            StartAt = startAt,
            EndAt = endAt
        });

        _repository.Verify(
            r => r.Add(It.Is<Event>(e =>
                e.Id == result.Id &&
                e.Title == dto.Title &&
                e.Description == dto.Description &&
                e.StartAt == startAt.UtcDateTime &&
                e.EndAt == endAt.UtcDateTime)),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_InvalidTitle_ThrowsDomainException(string? title)
    {
        // Arrange
        CreateEventDto dto = new()
        {
            Title = title!,
            Description = "Invalid event",
            StartAt = UtcDate(2026, 6, 1, 10),
            EndAt = UtcDate(2026, 6, 1, 11)
        };

        // Act
        Action act = () => _service.Create(dto);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Where(e => e.Property == nameof(Event.Title));

        _repository.Verify(r => r.Add(It.IsAny<Event>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 9)]
    [InlineData(10, 10)]
    public void Create_InvalidDateRange_ThrowsDomainException(int startHour, int endHour)
    {
        // Arrange
        CreateEventDto dto = new()
        {
            Title = "Invalid dates",
            Description = null,
            StartAt = UtcDate(2026, 6, 1, startHour),
            EndAt = UtcDate(2026, 6, 1, endHour)
        };

        // Act
        Action act = () => _service.Create(dto);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Where(e => e.Property == nameof(Period.EndAt));

        _repository.Verify(r => r.Add(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public void Update_EventExists_UpdatesEventAndReturnsDto()
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

        Guid id = Guid.NewGuid();
        Event existing = CreateEvent(id);

        _repository.Setup(r => r.GetById(id)).Returns(existing);

        // Act
        EventDto result = _service.Update(id, dto);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Id = id,
            dto.Title,
            dto.Description,
            StartAt = startAt,
            EndAt = endAt
        });

        _repository.Verify(
            r => r.Update(It.Is<Event>(e =>
                e.Id == id &&
                e.Title == dto.Title &&
                e.Description == dto.Description &&
                e.StartAt == startAt.UtcDateTime &&
                e.EndAt == endAt.UtcDateTime)),
            Times.Once);
    }

    [Fact]
    public void Update_EventDoesNotExist_ThrowsNotFoundException()
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

        _repository.Setup(r => r.GetById(id)).Returns((Event?)null);

        // Act
        Action act = () => _service.Update(id, dto);

        // Assert
        act.Should().Throw<NotFoundException>();

        _repository.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 9)]
    [InlineData(10, 10)]
    public void Update_InvalidDateRange_ThrowsDomainException(int startHour, int endHour)
    {
        // Arrange  
        Guid id = Guid.NewGuid();
        UpdateEventDto dto = new()
        {
            Title = "Invalid dates",
            Description = null,
            StartAt = UtcDate(2026, 6, 1, startHour),
            EndAt = UtcDate(2026, 6, 1, endHour)
        };

        // Act
        Action act = () => _service.Update(id, dto);

        // Assert
        act.Should()
            .Throw<DomainException>()
            .Where(e => e.Property == nameof(Period.EndAt));

        _repository.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public void GetById_EventExists_ReturnsEvent()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Event existing = CreateEvent(id, UtcDateTime(2026, 6, 3, 10), UtcDateTime(2026, 6, 3, 11));

        DateTimeOffset expectedStartAt = new(existing.StartAt, TimeSpan.Zero);
        DateTimeOffset expectedEndAt = new(existing.EndAt, TimeSpan.Zero);

        _repository.Setup(r => r.GetById(id)).Returns(existing);

        // Act
        EventDto result = _service.GetById(id);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            existing.Id,
            existing.Title,
            existing.Description,
            StartAt = expectedStartAt,
            EndAt = expectedEndAt,
        });
    }

    [Fact]
    public void GetById_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        _repository.Setup(r => r.GetById(id)).Returns((Event?)null);

        // Act
        Action act = () => _service.GetById(id);

        // Assert
        act.Should()
            .Throw<NotFoundException>()
            .Where(e => e.EntityId.Equals(id) && e.EntityName == nameof(Event));
    }

    [Fact]
    public void GetAll_EventsExist_ReturnsDataAndPaginationMetadata()
    {
        // Arrange
        Event first = CreateEvent(Guid.NewGuid());
        Event second = CreateEvent(Guid.NewGuid());

        _repository.Setup(r => r.Count(It.IsAny<EventFilter>())).Returns(5);
        _repository.Setup(r => r.GetAll(It.IsAny<EventFilter>(), 2, 2)).Returns([first, second]);

        GetEventsDto dto = new() { Page = 2, PageSize = 2 };

        // Act
        PaginatedResult<EventDto> result = _service.GetAll(dto);

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
            .Equal(first.Id, second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("meetup")]
    public void GetAll_TitleFilterProvided_PassesTitleFilterToRepository(string? title)
    {
        // Arrange
        SetupEmptyGetAll();

        // Act
        _service.GetAll(new GetEventsDto { Title = title });

        // Assert
        _repository.Verify(r => r.Count(It.Is<EventFilter>(f => f.Title == title)), Times.Once);
        _repository.Verify(r => r.GetAll(It.Is<EventFilter>(f => f.Title == title), 1, 10), Times.Once);
    }

    [Fact]
    public void GetAll_DateFiltersProvided_PassesUtcDateFiltersToRepository()
    {
        // Arrange
        SetupEmptyGetAll();

        DateTimeOffset from = new(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(3));
        DateTimeOffset to = new(2026, 6, 10, 18, 0, 0, TimeSpan.FromHours(3));

        GetEventsDto dto = new() { From = from, To = to };

        // Act
        _service.GetAll(dto);

        // Assert
        _repository.Verify(
            r => r.GetAll(
                It.Is<EventFilter>(f => f.From == from.UtcDateTime && f.To == to.UtcDateTime),
                1,
                10),
            Times.Once);
    }

    [Fact]
    public void GetAll_FromAfterTo_ThrowsValidationException()
    {
        // Arrange
        GetEventsDto dto = new()
        {
            From = UtcDate(2026, 7, 2, 10),
            To = UtcDate(2026, 7, 1, 10)
        };

        // Act
        Action act = () => _service.GetAll(dto);

        // Assert
        act.Should()
            .Throw<ValidationException>()
            .Where(e => e.Errors.ContainsKey(nameof(GetEventsDto.From)));

        _repository.Verify(r => r.Count(It.IsAny<EventFilter>()), Times.Never);
        _repository.Verify(r => r.GetAll(It.IsAny<EventFilter>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(-10, 25, 1, 25)]
    [InlineData(0, 25, 1, 25)]
    [InlineData(2, 0, 2, 1)]
    [InlineData(2, -10, 2, 1)]
    [InlineData(1, Pagination.MaxPageSize + 1, 1, Pagination.MaxPageSize)]
    public void GetAll_PaginationValuesProvided_NormalizesPagination(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        // Arrange
        SetupEmptyGetAll();

        GetEventsDto dto = new() { Page = page, PageSize = pageSize };

        // Act
        PaginatedResult<EventDto> result = _service.GetAll(dto);

        // Assert
        result.PageNumber.Should().Be(expectedPage);
        result.PageSize.Should().Be(expectedPageSize);

        _repository.Verify(r => r.Count(It.IsAny<EventFilter>()), Times.Once);
        _repository.Verify(r => r.GetAll(It.IsAny<EventFilter>(), expectedPage, expectedPageSize), Times.Once);
    }

    [Fact]
    public void GetAll_CombinedFilterProvided_PassesAllFiltersAndPaginationToRepository()
    {
        // Arrange
        SetupEmptyGetAll();

        DateTimeOffset from = UtcDate(2026, 8, 1, 9);
        DateTimeOffset to = UtcDate(2026, 8, 31, 18);

        // Act
        _service.GetAll(new GetEventsDto
        {
            Title = "conference",
            From = from,
            To = to,
            Page = 3,
            PageSize = 4
        });

        // Assert
        _repository.Verify(
            r => r.GetAll(
                It.Is<EventFilter>(f =>
                    f.Title == "conference" &&
                    f.From == from.UtcDateTime &&
                    f.To == to.UtcDateTime),
                3,
                4),
            Times.Once);
    }

    [Fact]
    public void Delete_EventExists_DeletesEvent()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        Event existing = CreateEvent(id);

        _repository.Setup(r => r.GetById(id)).Returns(existing);

        // Act
        _service.Delete(id);

        // Assert
        _repository.Verify(r => r.Delete(id), Times.Once);
    }

    [Fact]
    public void Delete_EventDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        _repository.Setup(r => r.GetById(id)).Returns((Event?)null);

        // Act
        Action act = () => _service.Delete(id);

        // Assert
        act.Should()
            .Throw<NotFoundException>()
            .Where(e => e.EntityId.Equals(id) && e.EntityName == nameof(Event));

        _repository.Verify(r => r.Delete(It.IsAny<Guid>()), Times.Never);
    }

    private void SetupEmptyGetAll()
    {
        _repository.Setup(r => r.Count(It.IsAny<EventFilter>())).Returns(0);
        _repository.Setup(r => r.GetAll(It.IsAny<EventFilter>(), It.IsAny<int>(), It.IsAny<int>())).Returns([]);
    }

    private static Event CreateEvent(Guid id, DateTime? startAt = null, DateTime? endAt = null)
    {
        DateTime start = startAt ?? DateTime.MinValue;
        DateTime end = endAt ?? DateTime.MaxValue;

        Period period = new(start, end);

        return new(id, "Event title", "Event description", period);
    }

    private static DateTimeOffset UtcDate(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    private static DateTime UtcDateTime(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}