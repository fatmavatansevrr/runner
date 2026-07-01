using System.Text.Json;
using FirebaseAdmin.Auth;
using RunningApp.Api.Logging;
using RunningApp.Application.Identity;
using RunningApp.Application.Services;

namespace RunningApp.Api.Auth;

/// <summary>
/// Validates the Firebase ID Token on every incoming request.
///
/// Flow:
///   No Authorization header → pass through. Controllers that call
///   ICurrentUserAccessor will receive an UnauthorizedAppException (→ 401)
///   from FirebaseIdentityProvider because no identity is stored.
///
///   Authorization: Bearer valid_token → verifies with Firebase Admin SDK,
///   stores AuthenticatedIdentity in HttpContext.Items, then upserts the
///   backend UserProfile via UserSynchronizationService.
///
///   Authorization: Bearer invalid_or_expired_token → short-circuits with 401.
///   The request never reaches a controller.
///
/// Health endpoints have no ICurrentUserAccessor call so they pass through
/// regardless of whether a token is present.
/// </summary>
public sealed class FirebaseAuthMiddleware
{
    internal const string ItemsKey = "FirebaseAuthIdentity";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthMiddleware> _logger;

    public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> logger)
    {
        _next  = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (authHeader is not null
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var idToken = authHeader["Bearer ".Length..].Trim();

            FirebaseToken decoded;
            try
            {
                decoded = await FirebaseAuth.DefaultInstance
                    .VerifyIdTokenAsync(idToken, context.RequestAborted);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Firebase token validation failed ({ErrorCode}): {Path}",
                    CorrelationIdAccessor.GetOrCreate(context),
                    ex.AuthErrorCode,
                    context.Request.Path);

                await WriteUnauthorizedAsync(
                    context,
                    "Your session has expired. Please sign in again.");
                return;
            }

            // Extract the claims that come from Firebase — user ID is the
            // authoritative source; email and name may be absent for some
            // providers (e.g. anonymous or phone).
            decoded.Claims.TryGetValue("email",          out var emailClaim);
            decoded.Claims.TryGetValue("name",           out var nameClaim);
            decoded.Claims.TryGetValue("picture",        out var pictureClaim);
            decoded.Claims.TryGetValue("email_verified", out var verifiedClaim);

            var isEmailVerified = verifiedClaim is bool b ? b
                : bool.TryParse(verifiedClaim?.ToString(), out var parsed) && parsed;

            var identity = new AuthenticatedIdentity
            {
                UserId        = decoded.Uid,
                Email         = emailClaim?.ToString(),
                Name          = nameClaim?.ToString(),
                PhotoUrl      = pictureClaim?.ToString(),
                EmailVerified = isEmailVerified,
            };

            // Upsert Users + UserProfile rows. SynchronizeAsync returns the
            // Users row so we can store the internal UUID alongside the
            // Firebase UID for downstream services that need it.
            var syncService = context.RequestServices
                .GetRequiredService<IUserSynchronizationService>();

            var user = await syncService.SynchronizeAsync(
                identity.UserId,
                identity.Name,
                identity.Email,
                identity.PhotoUrl,
                identity.EmailVerified,
                context.RequestAborted);

            identity.InternalUserId = user.Id;
            context.Items[ItemsKey] = identity;
        }

        await _next(context);
    }

    /// <summary>Returns the identity stored by this middleware, or null if the
    /// request arrived without a token (public or unauthenticated path).</summary>
    internal static AuthenticatedIdentity? GetIdentity(HttpContext context)
        => context.Items.TryGetValue(ItemsKey, out var v) ? v as AuthenticatedIdentity : null;

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        var correlationId = CorrelationIdAccessor.GetOrCreate(context);

        context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new { errorCode = "UNAUTHORIZED", message, correlationId },
            JsonOptions,
            context.RequestAborted);
    }
}
