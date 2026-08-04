namespace EventHub.Api.Domain.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);