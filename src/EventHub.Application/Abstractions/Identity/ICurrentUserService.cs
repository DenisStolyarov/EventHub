namespace EventHub.Application.Abstractions.Identity;

public interface ICurrentUserService
{
    public Guid? Id { get; }

    bool IsInRole(string role);
}
