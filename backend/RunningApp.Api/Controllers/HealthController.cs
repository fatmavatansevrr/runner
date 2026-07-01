using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunningApp.Persistence;

namespace RunningApp.Api.Controllers;

/// <summary>
/// Development diagnostics only — not part of the mobile app's contract.
/// Intentionally outside the versioned /api/v1 prefix, matching standard
/// health-check conventions (load balancers, uptime probes, etc.).
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>GET /health — process liveness only, no DB round-trip.</summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy" });
    }

    /// <summary>GET /health/database — checks connectivity and pending migrations.</summary>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth(CancellationToken ct)
    {
        bool canConnect;
        try
        {
            canConnect = await _context.Database.CanConnectAsync(ct);
        }
        catch
        {
            canConnect = false;
        }

        var pendingMigrations = 0;
        if (canConnect)
        {
            var pending = await _context.Database.GetPendingMigrationsAsync(ct);
            pendingMigrations = pending.Count();
        }

        return Ok(new
        {
            database = canConnect ? "healthy" : "unhealthy",
            can_connect = canConnect,
            pending_migrations = pendingMigrations,
        });
    }
}
