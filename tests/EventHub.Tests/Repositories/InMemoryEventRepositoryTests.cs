using EventHub.Api.Domain.Entities;
using EventHub.Api.Domain.Filters;
using EventHub.Api.Domain.ValueObjects;
using EventHub.Api.Infrastructure.Repositories;
using FluentAssertions;

namespace EventHub.Tests.Repositories;

public class InMemoryEventRepositoryTests
{
    private static readonly Guid MeetupId = Guid.NewGuid();
    private static readonly Guid WorkshopId = Guid.NewGuid();
    private static readonly Guid ConferenceId = Guid.NewGuid();
    private static readonly Guid WebinarId = Guid.NewGuid();

    private readonly InMemoryEventRepository _repository = new();

    [Theory]
    [InlineData("meetup")]
    [InlineData("MEETUP")]
    [InlineData("Meetup")]
    [InlineData("conference")]
    [InlineData("CONFERENCE")]
    [InlineData("Conference")]
    public void GetAll_TitleFilterProvided_FindsMatchingEventsCaseInsensitive(string title)
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new() { Title = title };

        // Act
        IEnumerable<Event> result = _repository.GetAll(filter, pageNumber: 1, pageSize: 10);

        // Assert
        result.Should().NotBeEmpty();
        result.Select(e => e.Title)
            .Should().OnlyContain(t => t.Contains(title, StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public void GetAll_TitleFilterProvided_FindsExactTitleMatchCaseInsensitive()
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new() { Title = "Cloud Conference" };

        // Act
        IEnumerable<Event> result = _repository.GetAll(filter, pageNumber: 1, pageSize: 10);

        // Assert
        result.Select(e => e.Id).Should().Equal(ConferenceId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetAll_EmptyTitleFilterProvided_ReturnsAllEvents(string? title)
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new() { Title = title };

        // Act
        IEnumerable<Event> result = _repository.GetAll(filter, pageNumber: 1, pageSize: 10);

        // Assert
        result.Select(e => e.Id)
            .Should()
            .Equal(MeetupId, WorkshopId, ConferenceId, WebinarId);
    }

    [Fact]
    public void GetAll_DateFiltersProvided_ReturnsEventsWithinDateRange()
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new()
        {
            From = UtcDateTime(2026, 6, 2, 0),
            To = UtcDateTime(2026, 6, 10, 23)
        };

        // Act
        IEnumerable<Event> result = _repository.GetAll(filter, pageNumber: 1, pageSize: 10);

        // Assert
        result.Select(e => e.Id)
            .Should()
            .Equal(WorkshopId, ConferenceId);
    }

    [Fact]
    public void GetAll_CombinedFilterProvided_ReturnsMatchingEvents()
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new()
        {
            Title = "conference",
            From = UtcDateTime(2026, 6, 1, 0),
            To = UtcDateTime(2026, 6, 30, 23)
        };

        // Act
        IEnumerable<Event> result = _repository.GetAll(filter, pageNumber: 1, pageSize: 10);

        // Assert
        result.Select(e => e.Id)
            .Should()
            .Equal(ConferenceId);
    }

    [Fact]
    public void GetAll_PaginationProvided_ReturnsRequestedPage()
    {
        // Arrange
        SeedEvents();

        // Act
        IEnumerable<Event> result = _repository.GetAll(new(), pageNumber: 2, pageSize: 2);

        // Assert
        result.Select(e => e.Id)
            .Should()
            .Equal(ConferenceId, WebinarId);
    }

    [Fact]
    public void Count_FilterProvided_ReturnsMatchingEventsCount()
    {
        // Arrange
        SeedEvents();

        EventFilter filter = new()
        {
            From = UtcDateTime(2026, 6, 1, 0),
            To = UtcDateTime(2026, 6, 10, 23)
        };

        // Act
        int result = _repository.Count(filter);

        // Assert
        result.Should().Be(3);
    }

    private void SeedEvents()
    {
        _repository.Add(CreateEvent(MeetupId, "Community Meetup", UtcDateTime(2026, 6, 1, 10), UtcDateTime(2026, 6, 1, 12)));
        _repository.Add(CreateEvent(WorkshopId, "Architecture Workshop", UtcDateTime(2026, 6, 5, 9), UtcDateTime(2026, 6, 5, 11)));
        _repository.Add(CreateEvent(ConferenceId, "Cloud Conference", UtcDateTime(2026, 6, 10, 14), UtcDateTime(2026, 6, 10, 18)));
        _repository.Add(CreateEvent(WebinarId, "Online Webinar", UtcDateTime(2026, 7, 1, 16), UtcDateTime(2026, 7, 1, 17)));
    }

    private static Event CreateEvent(Guid id, string title, DateTime startAt, DateTime endAt)
    {
        Period period = new(startAt, endAt);

        return new Event(id, title, $"{title} description", period);
    }

    private static DateTime UtcDateTime(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}