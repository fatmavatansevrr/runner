using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.Profile;
using RunningApp.Application.Services;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private const string MockUserId = "mock-user-001";

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>GET /api/v1/profile/overview</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ProfileOverviewResponse), 200)]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var response = await _profileService.GetProfileOverviewAsync(MockUserId, ct);
        return Ok(response);
    }
}
