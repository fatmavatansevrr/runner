using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.PendingConfirmation;
using RunningApp.Application.Services;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/pending-confirmations")]
public class PendingConfirmationsController : ControllerBase
{
    private readonly IPendingConfirmationService _pendingConfirmationService;
    private readonly ICurrentUserAccessor _currentUser;

    public PendingConfirmationsController(IPendingConfirmationService pendingConfirmationService, ICurrentUserAccessor currentUser)
    {
        _pendingConfirmationService = pendingConfirmationService;
        _currentUser = currentUser;
    }

    /// <summary>GET /api/v1/pending-confirmations</summary>
    [HttpGet]
    [ProducesResponseType(typeof(System.Collections.Generic.List<PendingConfirmationResponse>), 200)]
    public async Task<IActionResult> GetPendingConfirmations(CancellationToken ct)
    {
        var response = await _pendingConfirmationService.GetPendingConfirmationsAsync(_currentUser.InternalUserId, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/pending-confirmations/resolve</summary>
    [HttpPost("resolve")]
    [ProducesResponseType(typeof(ResolvePendingConfirmationResponse), 200)]
    public async Task<IActionResult> Resolve([FromBody] ResolvePendingConfirmationRequest request, CancellationToken ct)
    {
        var response = await _pendingConfirmationService.ResolvePendingConfirmationAsync(_currentUser.InternalUserId, request, ct);
        return Ok(response);
    }
}
