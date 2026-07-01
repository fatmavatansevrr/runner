using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using RunningApp.Application.Services;
using RunningApp.Persistence;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/testing")]
public class TestingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ICurrentUserAccessor _currentUser;

    public TestingController(AppDbContext context, IWebHostEnvironment env, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _env = env;
        _currentUser = currentUser;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabase(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return StatusCode(403, new { message = "Testing endpoints are only available in Development mode." });
        }

        var internalUserId = _currentUser.InternalUserId;

        var planIds = await _context.TrainingPlans
            .Where(x => x.InternalUserId == internalUserId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        // Delete in FK-safe order:
        // 1. Tables with RESTRICT FKs pointing to TrainingDays must go first.
        var logs = _context.WorkoutLogs.Where(x => x.InternalUserId == internalUserId);
        _context.WorkoutLogs.RemoveRange(logs);

        var decisions = _context.NotTodayDecisions.Where(x => x.InternalUserId == internalUserId);
        _context.NotTodayDecisions.RemoveRange(decisions);

        var pending = _context.PendingConfirmations.Where(x => x.InternalUserId == internalUserId);
        _context.PendingConfirmations.RemoveRange(pending);

        // 2. Tables with RESTRICT FKs pointing to TrainingPlans.
        var adaptationEvents = _context.AdaptationEvents.Where(x => x.InternalUserId == internalUserId);
        _context.AdaptationEvents.RemoveRange(adaptationEvents);

        // 3. PlanEvents has no FK constraint — can go any time.
        var events = _context.PlanEvents.Where(x => x.InternalUserId == internalUserId);
        _context.PlanEvents.RemoveRange(events);

        var previews = _context.PlanPreviews.Where(x => x.InternalUserId == internalUserId);
        _context.PlanPreviews.RemoveRange(previews);

        // 4. TrainingDays before TrainingPlans — TrainingDay has a direct RESTRICT
        //    FK to TrainingPlan separate from the TrainingWeek→TrainingDay cascade.
        var days = _context.TrainingDays.Where(d => planIds.Contains(d.PlanId));
        _context.TrainingDays.RemoveRange(days);

        var weeks = _context.TrainingWeeks.Where(w => planIds.Contains(w.PlanId));
        _context.TrainingWeeks.RemoveRange(weeks);

        var plans = _context.TrainingPlans.Where(x => x.InternalUserId == internalUserId);
        _context.TrainingPlans.RemoveRange(plans);

        // 5. UserProfile last — the auth middleware recreates it on the
        //    next request so the user lands back at onboarding.
        var profiles = _context.UserProfiles.Where(x => x.InternalUserId == internalUserId);
        _context.UserProfiles.RemoveRange(profiles);

        await _context.SaveChangesAsync(ct);

        return Ok(new { message = $"Database cleared for internal user '{internalUserId}'. Active plan, progress, and logs have been reset." });
    }
}
