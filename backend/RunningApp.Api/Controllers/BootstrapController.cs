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
    private const string MockUserId = "mock-user-001";

    public BootstrapController(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BootstrapResponse), 200)]
    public async Task<IActionResult> GetBootstrap(CancellationToken ct)
    {
        var response = await _bootstrapService.GetBootstrapAsync(MockUserId, ct);
        return Ok(response);
    }
}
