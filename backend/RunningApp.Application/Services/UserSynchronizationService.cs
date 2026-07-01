using Microsoft.EntityFrameworkCore;
using RunningApp.Domain.Entities;
using RunningApp.Domain.Enums;
using RunningApp.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public interface IUserSynchronizationService
{
    /// <summary>
    /// Ensures a Users row and a linked UserProfile row exist for the given
    /// Firebase identity. Creates them on first login; refreshes identity
    /// fields (DisplayName, Email, PhotoUrl, EmailVerified) on subsequent
    /// requests if the values changed upstream.
    ///
    /// Never touches user preferences (Unit) or any TrainingPlan data.
    /// Returns the upserted User so the caller can store the internal UUID.
    /// </summary>
    Task<User> SynchronizeAsync(
        string firebaseUid,
        string? displayName,
        string? email,
        string? photoUrl,
        bool emailVerified,
        CancellationToken ct = default);
}

public sealed class UserSynchronizationService : IUserSynchronizationService
{
    private readonly AppDbContext _context;

    public UserSynchronizationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> SynchronizeAsync(
        string firebaseUid,
        string? displayName,
        string? email,
        string? photoUrl,
        bool emailVerified,
        CancellationToken ct = default)
    {
        // ── 1. Upsert Users row ──────────────────────────────────────────────
        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(
                u => u.ExternalAuthProvider == "firebase" && u.ExternalUserId == firebaseUid,
                ct);

        if (user == null)
        {
            user = new User
            {
                Id                   = Guid.NewGuid(),
                ExternalAuthProvider = "firebase",
                ExternalUserId       = firebaseUid,
                DisplayName          = string.IsNullOrWhiteSpace(displayName) ? "Runner" : displayName,
                Email                = email,
                PhotoUrl             = photoUrl,
                EmailVerified        = emailVerified,
                CreatedAt            = DateTime.UtcNow,
                UpdatedAt            = DateTime.UtcNow,
            };
            _context.Users.Add(user);
        }
        else
        {
            var changed = false;

            if (!string.IsNullOrWhiteSpace(displayName) && user.DisplayName != displayName)
            {
                user.DisplayName = displayName;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
            {
                user.Email = email;
                changed = true;
            }
            if (photoUrl != null && user.PhotoUrl != photoUrl)
            {
                user.PhotoUrl = photoUrl;
                changed = true;
            }
            if (user.EmailVerified != emailVerified)
            {
                user.EmailVerified = emailVerified;
                changed = true;
            }

            if (changed)
                user.UpdatedAt = DateTime.UtcNow;
        }

        // ── 2. Upsert UserProfile row ────────────────────────────────────────
        if (user.Profile == null)
        {
            var profile = new UserProfile
            {
                Id             = Guid.NewGuid(),
                InternalUserId = user.Id,
                Unit           = DistanceUnit.Km,
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow,
            };
            _context.UserProfiles.Add(profile);
        }

        await _context.SaveChangesAsync(ct);
        return user;
    }
}
