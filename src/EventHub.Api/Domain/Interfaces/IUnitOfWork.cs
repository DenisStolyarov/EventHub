namespace EventHub.Api.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IEventRepository Events { get; }

    IBookingRepository Bookings { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}