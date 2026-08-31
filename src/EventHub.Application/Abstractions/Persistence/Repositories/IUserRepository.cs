using EventHub.Domain.Entities;

namespace EventHub.Application.Abstractions.Persistence.Repositories;

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

    void Add(User user);
}
