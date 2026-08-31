using EventHub.Domain.Enums;

namespace EventHub.Application.Constants;

public static class UserRoles
{
    public static string Admin { get; } = UserRole.Admin.ToString();

    public static string User { get; } = UserRole.User.ToString();
}
