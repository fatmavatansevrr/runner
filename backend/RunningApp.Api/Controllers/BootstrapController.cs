using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.Bootstrap;
using RunningApp.Application.Services;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
public class BootstrapController : ControllerBase
{
    private readonly IBootstrapService _bootstrapService;
    private readonly ICurrentUserAccessor _currentUser;

    public BootstrapController(IBootstrapService bootstrapService, ICurrentUserAccessor currentUser)
    {
        _bootstrapService = bootstrapService;
        _currentUser = currentUser;
    }

    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BootstrapResponse), 200)]
    public async Task<IActionResult> GetBootstrap(CancellationToken ct)
    {
        var response = await _bootstrapService.GetBootstrapAsync(_currentUser.InternalUserId, ct);
        return Ok(response);
    }
}
