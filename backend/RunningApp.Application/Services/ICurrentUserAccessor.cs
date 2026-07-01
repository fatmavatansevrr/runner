using RunningApp.Application.Identity;

namespace RunningApp.Application.Services;

/// <summary>
/// Resolves the current user's identity for the request. Business services
/// and controllers depend only on this interface.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Firebase UID — kept for logging and legacy compat.</summary>
    string UserId { get; }

    /// <summary>Internal Users.Id UUID — use for all DB queries.</summary>
    Guid InternalUserId { get; }
}

public sealed class MockCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IIdentityProvider _identityProvider;

    public MockCurrentUserAccessor(IIdentityProvider identityProvider)
    {
        _identityProvider = identityProvider;
    }

    public string UserId => _identityProvider.GetCurrentIdentity().UserId;
    public Guid InternalUserId => _identityProvider.GetCurrentIdentity().InternalUserId;
}
