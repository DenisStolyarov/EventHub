using EventHub.Application.Abstractions.Persistence;
using EventHub.Application.Common;
using EventHub.Application.Filters;
using EventHub.Domain.Entities;
using EventHub.Domain.ValueObjects;
using EventHub.Infrastructure.Persistence;
using EventHub.IntegrationTests.Abstractions;
using EventHub.IntegrationTests.Fixtures;
using EventHub.IntegrationTests.Providers;
using FluentAssertions;

namespace EventHub.IntegrationTests.Repositories;

public sealed class EventRepositoryTests(IntegrationTestFixture fixture) : RepositoryTestBase(fixture), IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetByIdAsync_ReturnsEvent_WhenExists()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Event? result = await uow.Events.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(@event.Id);
        result.Title.Should().Be(@event.Title);
        result.TotalSeats.Should().Be(@event.TotalSeats);
        result.AvailableSeats.Should().Be(@event.AvailableSeats);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        Event? result = await uow.Events.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Add_PersistsEvent()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();

        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        // Act
        uow.Events.Add(@event);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Event? stored = await verifyCtx.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.Id.Should().Be(@event.Id);
        stored.Title.Should().Be(@event.Title);
        stored.TotalSeats.Should().Be(@event.TotalSeats);
        stored.AvailableSeats.Should().Be(@event.AvailableSeats);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        // Arrange
        const string newTitle = "Updated Title";
        const string newDescription = "Updated Description";

        Event @event = EntityProvider.CreateEvent();
        Period newPeriod = new(
            new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 17, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Event tracked = (await uow.Events.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken))!;

        // Act
        tracked.Update(newTitle, newDescription, newPeriod);

        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Event? stored = await verifyCtx.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored.Title.Should().Be(newTitle);
        stored.Description.Should().Be(newDescription);
        stored.StartAt.Should().Be(newPeriod.StartAt);
        stored.EndAt.Should().Be(newPeriod.EndAt);
    }

    [Fact]
    public async Task Delete_RemovesEvent()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Event tracked = (await uow.Events.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken))!;

        // Act
        uow.Events.Delete(tracked);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Event? stored = await verifyCtx.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        stored.Should().BeNull();
    }

    [Fact]
    public async Task Delete_CascadesToBookings()
    {
        // Arrange
        Event @event = EntityProvider.CreateEvent();
        Booking booking1 = EntityProvider.CreateBooking(@event.Id);
        Booking booking2 = EntityProvider.CreateBooking(@event.Id);

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(@event);
        ctx.Bookings.AddRange(booking1, booking2);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Event? tracked = await uow.Events.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken);

        // Act
        uow.Events.Delete(tracked!);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using AppDbContext verifyCtx = Fixture.CreateContext();

        Event? storedEvent = await verifyCtx.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);

        storedEvent.Should().BeNull();

        Booking? storedBooking1 = await verifyCtx.Bookings.FindAsync([booking1.Id], TestContext.Current.CancellationToken);
        Booking? storedBooking2 = await verifyCtx.Bookings.FindAsync([booking2.Id], TestContext.Current.CancellationToken);

        storedBooking1.Should().BeNull();
        storedBooking2.Should().BeNull();
    }

    [Fact]
    public async Task GetFilteredAsync_WithNoFilter_ReturnsAllEvents()
    {
        // Arrange
        EventFilter filter = new() { Page = 1, PageSize = 10 };

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(EntityProvider.CreateEvent(title: "Event 1"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Event 2"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Event 3"));
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_ReturnsMatchingEvents()
    {
        // Arrange
        EventFilter filter = new() { Title = "Community", Page = 1, PageSize = 10 };

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(EntityProvider.CreateEvent(title: "Community Meetup"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Architecture Workshop"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Community Talk"));
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(e => e.Title.Contains("Community"));
    }

    [Fact]
    public async Task GetFilteredAsync_WithFromFilter_ReturnsEventsStartingAtOrAfter()
    {
        // Arrange
        EventFilter filter = new()
        {
            From = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };
        Event early = EntityProvider.CreateEvent(
            startAt: new DateTime(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));
        Event late = EntityProvider.CreateEvent(
            startAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.AddRange(early, late);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(late.Id);
    }

    [Fact]
    public async Task GetFilteredAsync_WithToFilter_ReturnsEventsEndingAtOrBefore()
    {
        // Arrange
        EventFilter filter = new()
        {
            To = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };
        Event early = EntityProvider.CreateEvent(
            startAt: new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        Event late = EntityProvider.CreateEvent(
            startAt: new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.AddRange(early, late);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(early.Id);
    }

    [Fact]
    public async Task GetFilteredAsync_WithCombinedFilters_ReturnsMatchingEvents()
    {
        // Arrange
        EventFilter filter = new()
        {
            Title = "Target",
            From = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc),
            Page = 1,
            PageSize = 10
        };
        Event e1 = EntityProvider.CreateEvent(
            title: "Other conference",
            startAt: new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc));
        Event e2 = EntityProvider.CreateEvent(
            title: "Another workshop",
            startAt: new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc));
        Event target = EntityProvider.CreateEvent(
            title: "Target conference",
            startAt: new DateTime(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.AddRange(e1, e2, target);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task GetFilteredAsync_PaginatesCorrectly()
    {
        // Arrange
        EventFilter filter = new() { Page = 2, PageSize = 2 };
        Event e1 = EntityProvider.CreateEvent(title: "Event 1",
            startAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        Event e2 = EntityProvider.CreateEvent(title: "Event 2",
            startAt: new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc));
        Event e3 = EntityProvider.CreateEvent(title: "Event 3",
            startAt: new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc));
        Event e4 = EntityProvider.CreateEvent(title: "Event 4",
            startAt: new DateTime(2026, 6, 4, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc));
        Event e5 = EntityProvider.CreateEvent(title: "Event 5",
            startAt: new DateTime(2026, 6, 5, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.AddRange(e1, e2, e3, e4, e5);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> page2 = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        page2.TotalCount.Should().Be(5);
        page2.Items.Should().HaveCount(2);
        page2.Items[0].Id.Should().Be(e3.Id);
        page2.Items[1].Id.Should().Be(e4.Id);
    }

    [Fact]
    public async Task GetFilteredAsync_PageBeyondData_ReturnsEmptyItems()
    {
        // Arrange
        EventFilter filter = new() { Page = 100, PageSize = 10 };

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(EntityProvider.CreateEvent(title: "Event 1"));
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_TotalCount_MatchesUnfilteredCount()
    {
        // Arrange
        EventFilter filterAll = new() { Title = "Alpha", Page = 1, PageSize = 10 };
        EventFilter filterNone = new() { Title = "Delta", Page = 1, PageSize = 10 };

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.Add(EntityProvider.CreateEvent(title: "Alpha"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Beta Alpha"));
        ctx.Events.Add(EntityProvider.CreateEvent(title: "Gamma"));
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filterAll, TestContext.Current.CancellationToken);
        PagedResult<Event> empty = await uow.Events.GetFilteredAsync(filterNone, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        empty.TotalCount.Should().Be(0);
        empty.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_SortsByStartAtAscending_RegardlessOfInsertionOrder()
    {
        // Arrange
        EventFilter filter = new() { Page = 1, PageSize = 10 };
        Event late = EntityProvider.CreateEvent(title: "Late",
            startAt: new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc));
        Event early = EntityProvider.CreateEvent(title: "Early",
            startAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        Event middle = EntityProvider.CreateEvent(title: "Middle",
            startAt: new DateTime(2026, 6, 5, 10, 0, 0, DateTimeKind.Utc),
            endAt: new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc));

        await using AppDbContext ctx = Fixture.CreateContext();
        await using IUnitOfWork uow = Fixture.Create<IUnitOfWork>();

        ctx.Events.AddRange(late, early, middle);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        PagedResult<Event> result = await uow.Events.GetFilteredAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Id.Should().Be(early.Id);
        result.Items[1].Id.Should().Be(middle.Id);
        result.Items[2].Id.Should().Be(late.Id);
    }
}