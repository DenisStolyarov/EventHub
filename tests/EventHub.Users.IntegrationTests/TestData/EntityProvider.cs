using EventHub.Users.Domain.Entities;
using EventHub.Users.Domain.Enums;

namespace EventHub.Users.IntegrationTests.TestData;

public static class EntityProvider
{
    public static User CreateUser(string login = "testuser", UserRole role = UserRole.User) =>
        new(Guid.CreateVersion7(), login, "hashedpassword", role);
}
