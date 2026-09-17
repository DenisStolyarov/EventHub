using EventHub.Users.Domain.Entities;

namespace EventHub.Users.Application.Abstractions.Persistence.Repositories;

public interface IUserRepository
{
    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

    Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default);

    void Add(User user);
}
