namespace RunningApp.Application.Identity;

/// <summary>
/// The identity resolved for the current request, regardless of which
/// provider produced it (mock, Firebase, Supabase, JWT, ...). Business
/// services never see this type directly — they depend on
/// <see cref="RunningApp.Application.Services.ICurrentUserAccessor"/>.
///
/// InternalUserId is populated by UserSynchronizationService after the Users
/// row is upserted. It is Guid.Empty until that upsert completes.
/// </summary>
public sealed class AuthenticatedIdentity
{
    public required string UserId { get; init; }   // Firebase UID (external)
    public Guid InternalUserId { get; set; }        // Users.Id (internal) — set by sync
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? PhotoUrl { get; init; }
    public bool EmailVerified { get; init; }
}
