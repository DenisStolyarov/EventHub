using EventHub.Application.Constants;

namespace EventHub.Api.Presentation.Models.Events;

public sealed record GetEventsRequest
{
    public string? Title { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public int Page { get; init; } = Pagination.DefaultPage;

    public int PageSize { get; init; } = Pagination.DefaultPageSize;
}
