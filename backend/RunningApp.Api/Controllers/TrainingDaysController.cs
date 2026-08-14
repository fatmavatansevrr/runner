using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.TrainingDay;
using RunningApp.Application.DTOs.Plan;
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
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILongHorizonActiveReadModelProvider _rollingReadProvider;
    private readonly ILongHorizonRollingSessionMutationService _rollingMutationService;

    public TrainingDaysController(
        ITrainingDayService trainingDayService,
        IWorkoutCompletionService completionService,
        INotTodayService notTodayService,
        ICurrentUserAccessor currentUser,
        ILongHorizonActiveReadModelProvider rollingReadProvider,
        ILongHorizonRollingSessionMutationService rollingMutationService)
    {
        _trainingDayService = trainingDayService;
        _completionService = completionService;
        _notTodayService = notTodayService;
        _currentUser = currentUser;
        _rollingReadProvider = rollingReadProvider;
        _rollingMutationService = rollingMutationService;
    }

    /// <summary>Returns one authorized, currently executable rolling Long-Horizon session.</summary>
    [HttpGet("rolling/{sessionId:guid}")]
    [ProducesResponseType(typeof(LongHorizonRollingSessionDetailResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> GetRollingDetail(Guid sessionId, CancellationToken ct) =>
        Ok(await _rollingReadProvider.GetSessionDetailAsync(_currentUser.InternalUserId, sessionId, ct));

    /// <summary>Records one immutable completion outcome; exact replay is idempotent and never activates the next window.</summary>
    [HttpPost("rolling/{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(LongHorizonSessionMutationResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CompleteRolling(Guid sessionId, [FromBody] LongHorizonCompleteSessionRequest request, CancellationToken ct) =>
        Ok(await _rollingMutationService.CompleteAsync(_currentUser.InternalUserId, sessionId, request, ct));

    /// <summary>Records one direct durable not-today outcome without rescheduling or automatic activation.</summary>
    [HttpPost("rolling/{sessionId:guid}/not-today")]
    [ProducesResponseType(typeof(LongHorizonSessionMutationResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> MarkRollingNotToday(Guid sessionId, [FromBody] LongHorizonNotTodayRequest request, CancellationToken ct) =>
        Ok(await _rollingMutationService.MarkNotTodayAsync(_currentUser.InternalUserId, sessionId, request, ct));

    /// <summary>GET /api/v1/training-days/{trainingDayId}</summary>
    [HttpGet("{trainingDayId:guid}")]
    [ProducesResponseType(typeof(TrainingDayDetailResponse), 200)]
    public async Task<IActionResult> GetDetail(Guid trainingDayId, CancellationToken ct)
    {
        var response = await _trainingDayService.GetTrainingDayDetailAsync(_currentUser.InternalUserId, trainingDayId, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/training-days/{trainingDayId}/complete</summary>
    [HttpPost("{trainingDayId:guid}/complete")]
    [ProducesResponseType(typeof(CompleteWorkoutResponse), 200)]
    public async Task<IActionResult> Complete(Guid trainingDayId, [FromBody] CompleteWorkoutRequest request, CancellationToken ct)
    {
        var response = await _completionService.CompleteWorkoutAsync(_currentUser.InternalUserId, trainingDayId, request, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/training-days/{trainingDayId}/not-today-decisions</summary>
    [HttpPost("{trainingDayId:guid}/not-today-decisions")]
    [ProducesResponseType(typeof(CreateNotTodayDecisionResponse), 200)]
    public async Task<IActionResult> CreateNotTodayDecision(Guid trainingDayId, [FromBody] CreateNotTodayDecisionRequest request, CancellationToken ct)
    {
        var response = await _notTodayService.CreateNotTodayDecisionAsync(_currentUser.InternalUserId, trainingDayId, request, ct);
        return Ok(response);
    }
}
