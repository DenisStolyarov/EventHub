using EventHub.Api.Application.Constants;

namespace EventHub.Api.Application.Dto.Events;

public sealed record GetEventsDto
{
    public string? Title { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public int Page { get; init; } = Pagination.DefaultPage;

    public int PageSize { get; init; } = Pagination.DefaultPageSize;
}