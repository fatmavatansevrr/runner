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
    private readonly ICurrentUserAccessor _currentUser;

    public ProfileController(IProfileService profileService, ICurrentUserAccessor currentUser)
    {
        _profileService = profileService;
        _currentUser = currentUser;
    }

    /// <summary>GET /api/v1/profile/overview</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ProfileOverviewResponse), 200)]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var response = await _profileService.GetProfileOverviewAsync(_currentUser.InternalUserId, ct);
        return Ok(response);
    }
}
