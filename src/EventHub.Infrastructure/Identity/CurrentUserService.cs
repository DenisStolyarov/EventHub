using EventHub.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace EventHub.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor context) : ICurrentUserService
{
    private IHttpContextAccessor Context { get; } = context;

    public Guid? Id => Guid.TryParse(GetValue(JwtClaimTypes.Sub), out Guid id) ? id : null;

    public bool IsInRole(string role) => Context.HttpContext.User.IsInRole(role);

    private string? GetValue(string type) => Context.HttpContext.User.FindFirst(type)?.Value;
}
