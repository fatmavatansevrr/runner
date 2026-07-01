using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.TrainingDay;
using RunningApp.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/not-today-decisions")]
public class NotTodayDecisionsController : ControllerBase
{
    private readonly INotTodayService _notTodayService;
    private readonly ICurrentUserAccessor _currentUser;

    public NotTodayDecisionsController(INotTodayService notTodayService, ICurrentUserAccessor currentUser)
    {
        _notTodayService = notTodayService;
        _currentUser = currentUser;
    }

    /// <summary>POST /api/v1/not-today-decisions/{decisionId}/confirm</summary>
    [HttpPost("{decisionId:guid}/confirm")]
    [ProducesResponseType(typeof(ConfirmNotTodayDecisionResponse), 200)]
    public async Task<IActionResult> Confirm(Guid decisionId, [FromBody] ConfirmNotTodayDecisionRequest request, CancellationToken ct)
    {
        var response = await _notTodayService.ConfirmNotTodayDecisionAsync(_currentUser.InternalUserId, decisionId, request, ct);
        return Ok(response);
    }
}
