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
    private const string MockUserId = "mock-user-001";

    public NotTodayDecisionsController(INotTodayService notTodayService)
    {
        _notTodayService = notTodayService;
    }

    /// <summary>POST /api/v1/not-today-decisions/{decisionId}/confirm</summary>
    [HttpPost("{decisionId:guid}/confirm")]
    [ProducesResponseType(typeof(ConfirmNotTodayDecisionResponse), 200)]
    public async Task<IActionResult> Confirm(Guid decisionId, [FromBody] ConfirmNotTodayDecisionRequest request, CancellationToken ct)
    {
        var response = await _notTodayService.ConfirmNotTodayDecisionAsync(MockUserId, decisionId, request, ct);
        return Ok(response);
    }
}
