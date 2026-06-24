using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
    private const string TargetUserId = "mock-user-001";

    public TestingController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabase(CancellationToken ct)
    {
        // Enforce development-only safety check
        if (!_env.IsDevelopment())
        {
            return Forbid("Testing endpoints are only available in Development mode.");
        }

        // Delete all data associated with mock-user-001
        var profiles = _context.UserProfiles.Where(x => x.UserId == TargetUserId);
        _context.UserProfiles.RemoveRange(profiles);

        var plans = _context.TrainingPlans.Where(x => x.UserId == TargetUserId);
        _context.TrainingPlans.RemoveRange(plans);

        var logs = _context.WorkoutLogs.Where(x => x.UserId == TargetUserId);
        _context.WorkoutLogs.RemoveRange(logs);

        var decisions = _context.NotTodayDecisions.Where(x => x.UserId == TargetUserId);
        _context.NotTodayDecisions.RemoveRange(decisions);

        var pending = _context.PendingConfirmations.Where(x => x.UserId == TargetUserId);
        _context.PendingConfirmations.RemoveRange(pending);

        var events = _context.PlanEvents.Where(x => x.UserId == TargetUserId);
        _context.PlanEvents.RemoveRange(events);

        var adaptationEvents = _context.AdaptationEvents.Where(x => x.UserId == TargetUserId);
        _context.AdaptationEvents.RemoveRange(adaptationEvents);

        var previews = _context.PlanPreviews.Where(x => x.UserId == TargetUserId);
        _context.PlanPreviews.RemoveRange(previews);

        await _context.SaveChangesAsync(ct);

        return Ok(new { message = $"Database cleared for user '{TargetUserId}'. Active plan, progress, and logs have been reset." });
    }
}
