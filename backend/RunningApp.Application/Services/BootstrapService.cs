using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Bootstrap;
using RunningApp.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Application.Services;

public class BootstrapService : IBootstrapService
{
    private readonly AppDbContext _context;

    public BootstrapService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BootstrapResponse> GetBootstrapAsync(Guid internalUserId, CancellationToken ct = default)
    {
        // UserProfile is guaranteed to exist for any authenticated request because
        // the auth middleware → UserSynchronizationService runs first and upserts it.
        var hasProfile = await _context.UserProfiles
            .AnyAsync(u => u.InternalUserId == internalUserId, ct);

        if (!hasProfile)
        {
            return new BootstrapResponse
            {
                IsAuthenticated = true,
                HasProfile = false,
                HasActivePlan = false,
                HasPendingConfirmations = false,
                NextScreen = "Onboarding"
            };
        }

        var hasPendingConfirmations = await _context.PendingConfirmations
            .AnyAsync(p => p.InternalUserId == internalUserId && p.Status == "pending", ct);

        if (hasPendingConfirmations)
        {
            return new BootstrapResponse
            {
                IsAuthenticated = true,
                HasProfile = true,
                HasActivePlan = true,
                HasPendingConfirmations = true,
                NextScreen = "PendingConfirmation"
            };
        }

        var hasActivePlan = await _context.TrainingPlans
            .AnyAsync(p => p.InternalUserId == internalUserId && p.Status == Domain.Enums.TrainingPlanStatus.Active, ct);

        return new BootstrapResponse
        {
            IsAuthenticated = true,
            HasProfile = true,
            HasActivePlan = hasActivePlan,
            HasPendingConfirmations = false,
            NextScreen = hasActivePlan ? "Home" : "NoActivePlan"
        };
    }
}
