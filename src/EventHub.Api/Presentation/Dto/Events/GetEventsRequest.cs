using System.ComponentModel.DataAnnotations;
using EventHub.Api.Application.Constants;

namespace EventHub.Api.Presentation.Dto.Events;

public sealed record GetEventsRequest
{
    public string? Title { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = Pagination.DefaultPage;

    [Range(1, Pagination.MaxPageSize)]
    public int PageSize { get; init; } = Pagination.DefaultPageSize;
}