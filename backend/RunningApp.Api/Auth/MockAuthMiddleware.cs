using RunningApp.Application.Identity;
using RunningApp.Application.Services;

namespace RunningApp.Api.Auth;

/// <summary>
/// Development-only middleware that replaces FirebaseAuthMiddleware in Mock auth mode.
/// Calls UserSynchronizationService to ensure a Users + UserProfile row exists for the
/// mock user, then stores an AuthenticatedIdentity (with InternalUserId populated) in
/// HttpContext.Items under the same key used by FirebaseAuthMiddleware so that
/// FirebaseIdentityProvider works identically in both auth paths.
/// </summary>
public sealed class MockAuthMiddleware
{
    private readonly RequestDelegate _next;

    public MockAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var syncService = context.RequestServices.GetRequiredService<IUserSynchronizationService>();
        var user = await syncService.SynchronizeAsync(
            MockIdentityProvider.MockUserId,
            "Mock User",
            "mock@local.dev",
            null,
            false,
            context.RequestAborted);

        var identity = new AuthenticatedIdentity
        {
            UserId         = MockIdentityProvider.MockUserId,
            Email          = "mock@local.dev",
            Name           = "Mock User",
            InternalUserId = user.Id,
        };

        context.Items[FirebaseAuthMiddleware.ItemsKey] = identity;
        await _next(context);
    }
}
