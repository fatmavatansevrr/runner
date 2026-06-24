using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.TrainingDay;
using RunningApp.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/training-days")]
public class TrainingDaysController : ControllerBase
{
    private readonly ITrainingDayService _trainingDayService;
    private readonly IWorkoutCompletionService _completionService;
    private readonly INotTodayService _notTodayService;

    private const string MockUserId = "mock-user-001";

    public TrainingDaysController(
        ITrainingDayService trainingDayService,
        IWorkoutCompletionService completionService,
        INotTodayService notTodayService)
    {
        _trainingDayService = trainingDayService;
        _completionService = completionService;
        _notTodayService = notTodayService;
    }

    /// <summary>GET /api/v1/training-days/{trainingDayId}</summary>
    [HttpGet("{trainingDayId:guid}")]
    [ProducesResponseType(typeof(TrainingDayDetailResponse), 200)]
    public async Task<IActionResult> GetDetail(Guid trainingDayId, CancellationToken ct)
    {
        var response = await _trainingDayService.GetTrainingDayDetailAsync(MockUserId, trainingDayId, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/training-days/{trainingDayId}/complete</summary>
    [HttpPost("{trainingDayId:guid}/complete")]
    [ProducesResponseType(typeof(CompleteWorkoutResponse), 200)]
    public async Task<IActionResult> Complete(Guid trainingDayId, [FromBody] CompleteWorkoutRequest request, CancellationToken ct)
    {
        var response = await _completionService.CompleteWorkoutAsync(MockUserId, trainingDayId, request, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/training-days/{trainingDayId}/not-today-decisions</summary>
    [HttpPost("{trainingDayId:guid}/not-today-decisions")]
    [ProducesResponseType(typeof(CreateNotTodayDecisionResponse), 200)]
    public async Task<IActionResult> CreateNotTodayDecision(Guid trainingDayId, [FromBody] CreateNotTodayDecisionRequest request, CancellationToken ct)
    {
        var response = await _notTodayService.CreateNotTodayDecisionAsync(MockUserId, trainingDayId, request, ct);
        return Ok(response);
    }
}
