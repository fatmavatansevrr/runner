using RunningApp.Application.Exceptions;
using RunningApp.Application.Identity;

namespace RunningApp.Api.Auth;

/// <summary>
/// Reads the Firebase-verified identity that <see cref="FirebaseAuthMiddleware"/>
/// stored in <c>HttpContext.Items</c> for this request.
///
/// If no token was present on the request (or the token was invalid and the
/// middleware already short-circuited with 401), this throws
/// <see cref="UnauthorizedAppException"/>, which the global exception handler
/// maps to HTTP 401.
///
/// Controllers and application services never reference this class directly;
/// they depend only on <see cref="ICurrentUserAccessor"/>.
/// </summary>
public sealed class FirebaseIdentityProvider : IIdentityProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FirebaseIdentityProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthenticatedIdentity GetCurrentIdentity()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "FirebaseIdentityProvider cannot be called outside of an HTTP request.");

        return FirebaseAuthMiddleware.GetIdentity(context)
            ?? throw new UnauthorizedAppException(
                "Authentication required. Please sign in.");
    }
}
