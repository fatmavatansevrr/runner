namespace RunningApp.Application.Identity;

/// <summary>
/// Resolves who is making the current request. This is the seam where a
/// real identity provider (Firebase, Supabase, a JWT bearer scheme, ...)
/// will plug in later. Nothing above <see cref="RunningApp.Application.Services.ICurrentUserAccessor"/>
/// — i.e. no controller or business service — should ever reference an
/// <see cref="IIdentityProvider"/> implementation directly.
/// </summary>
public interface IIdentityProvider
{
    AuthenticatedIdentity GetCurrentIdentity();
}
