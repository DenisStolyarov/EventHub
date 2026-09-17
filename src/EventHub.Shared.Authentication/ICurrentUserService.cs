namespace EventHub.Shared.Authentication;

public interface ICurrentUserService
{
    Guid? Id { get; }

    bool IsInRole(string role);
}
