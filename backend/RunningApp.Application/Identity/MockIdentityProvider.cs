namespace RunningApp.Application.Identity;

/// <summary>
/// Phase 1 placeholder identity provider. There is no real sign-in flow yet,
/// so every request resolves to the same seeded mock user. This is the only
/// place in the solution that should know the literal mock user id —
/// everything else goes through <see cref="IIdentityProvider"/> /
/// <see cref="RunningApp.Application.Services.ICurrentUserAccessor"/>.
///
/// To plug in real auth later (Firebase, Supabase, JWT, ...), implement
/// <see cref="IIdentityProvider"/> against the real provider and swap the
/// DI registration in Program.cs — no business service or controller needs
/// to change.
/// </summary>
public sealed class MockIdentityProvider : IIdentityProvider
{
    public const string MockUserId = "mock-user-001";

    public AuthenticatedIdentity GetCurrentIdentity() => new()
    {
        UserId = MockUserId,
        Email = "runner@example.com",
        Name = "Runner",
    };
}
