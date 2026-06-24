using Microsoft.EntityFrameworkCore;
using RunningApp.Application.DTOs.Bootstrap;
using RunningApp.Persistence;
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

    public async Task<BootstrapResponse> GetBootstrapAsync(string userId, CancellationToken ct = default)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (profile == null)
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

        // Check for pending confirmations
        var hasPendingConfirmations = await _context.PendingConfirmations
            .AnyAsync(p => p.UserId == userId && p.Status == "pending", ct);

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

        // Check for active plan
        var hasActivePlan = await _context.TrainingPlans
            .AnyAsync(p => p.UserId == userId && p.Status == Domain.Enums.TrainingPlanStatus.Active, ct);

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
