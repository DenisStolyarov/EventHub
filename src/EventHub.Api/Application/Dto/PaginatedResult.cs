namespace EventHub.Api.Application.Dto;

public sealed record PaginatedResult<T>
{
    public IEnumerable<T> Data { get; init; } = [];

    public int PageNumber { get; init; }
    
    public int PageSize { get; init; }
    
    public int TotalPages { get; init; }
    
    public int TotalRecords { get; init; }
    
    public int ItemsOnPage { get; init; }
    
    public bool HasNextPage => PageNumber < TotalPages;
    
    public bool HasPreviousPage => PageNumber > 1;
}