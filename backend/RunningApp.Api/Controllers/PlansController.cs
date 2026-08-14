using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.Commands.Plan;
using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Services;
using RunningApp.Application.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanPreviewService _previewService;
    private readonly IPlanConfirmationService _confirmationService;
    private readonly ILongHorizonPublicPlanService _longHorizonService;
    private readonly IPlanManagementService _managementService;
    private readonly IHomeQueryService _homeQueryService;
    private readonly ICalendarQueryService _calendarQueryService;
    private readonly ILongHorizonRollingWindowActivationService _rollingActivationService;
    private readonly ILongHorizonRollingRetryContinuationService _rollingRetryService;
    private readonly ICurrentUserAccessor _currentUser;

    public PlansController(
        IPlanPreviewService previewService,
        IPlanConfirmationService confirmationService,
        ILongHorizonPublicPlanService longHorizonService,
        IPlanManagementService managementService,
        IHomeQueryService homeQueryService,
        ICalendarQueryService calendarQueryService,
        ILongHorizonRollingWindowActivationService rollingActivationService,
        ILongHorizonRollingRetryContinuationService rollingRetryService,
        ICurrentUserAccessor currentUser)
    {
        _previewService = previewService;
        _confirmationService = confirmationService;
        _longHorizonService = longHorizonService;
        _managementService = managementService;
        _homeQueryService = homeQueryService;
        _calendarQueryService = calendarQueryService;
        _rollingActivationService = rollingActivationService;
        _rollingRetryService = rollingRetryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Generates a public-safe rolling preview for the authenticated
    /// TEN_K/Intermediate/4-day pilot with a 21-52 week structural horizon.
    /// The existing 8-20 week race route is intentionally unchanged.
    /// </summary>
    [HttpPost("generate-preview/race/long-horizon")]
    [ProducesResponseType(typeof(Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview.LongHorizonPlanPreviewContract), 200)]
    public async Task<IActionResult> GenerateLongHorizonRacePlanPreview(
        [FromBody] GenerateRacePlanPreviewRequest request, CancellationToken ct)
    {
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        var command = GeneratePreviewCommandMapper.ToCommand(request);
        var response = await _longHorizonService.GeneratePreviewAsync(_currentUser.InternalUserId, command, ct);
        return Ok(response);
    }

    /// <summary>
    /// Confirms a server-owned Long-Horizon preview. The client supplies no
    /// generated schedule or persistence identifiers.
    /// </summary>
    [HttpPost("confirm/long-horizon")]
    [ProducesResponseType(typeof(LongHorizonConfirmPlanResponse), 200)]
    public async Task<IActionResult> ConfirmLongHorizonPlan(
        [FromBody] LongHorizonConfirmPlanRequest request, CancellationToken ct)
    {
        var response = await _longHorizonService.ConfirmAsync(_currentUser.InternalUserId, request, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/generate-preview/race — returns a race plan preview</summary>
    [HttpPost("generate-preview/race")]
    [ProducesResponseType(typeof(GeneratePreviewResponse), 200)]
    public async Task<IActionResult> GenerateRacePlanPreview([FromBody] GenerateRacePlanPreviewRequest request, CancellationToken ct)
    {
        GenerateRacePlanPreviewRequestValidator.Validate(request);
        var command = GeneratePreviewCommandMapper.ToCommand(request);
        var response = await _previewService.GenerateRacePlanPreviewAsync(_currentUser.InternalUserId, command, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/generate-preview/habit — returns a habit plan preview</summary>
    [HttpPost("generate-preview/habit")]
    [ProducesResponseType(typeof(GeneratePreviewResponse), 200)]
    public async Task<IActionResult> GenerateHabitPlanPreview([FromBody] GenerateHabitPlanPreviewRequest request, CancellationToken ct)
    {
        GenerateHabitPlanPreviewRequestValidator.Validate(request);
        var command = GeneratePreviewCommandMapper.ToCommand(request);
        var response = await _previewService.GenerateHabitPlanPreviewAsync(_currentUser.InternalUserId, command, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/confirm — persists the confirmed plan</summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmPlanResponse), 200)]
    public async Task<IActionResult> ConfirmPlan([FromBody] ConfirmPlanRequest request, CancellationToken ct)
    {
        var response = await _confirmationService.ConfirmPlanAsync(_currentUser.InternalUserId, request, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/home</summary>
    [HttpGet("active/home")]
    [ProducesResponseType(typeof(HomeResponse), 200)]
    [ProducesResponseType(typeof(LongHorizonHomeResponse), 200)]
    public async Task<IActionResult> GetHome(CancellationToken ct)
    {
        var response = await _homeQueryService.GetHomeAsync(_currentUser.InternalUserId, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/calendar?month=YYYY-MM</summary>
    [HttpGet("active/calendar")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<TrainingDayResponse>), 200)]
    [ProducesResponseType(typeof(LongHorizonCalendarResponse), 200)]
    public async Task<IActionResult> GetCalendar([FromQuery] string month, CancellationToken ct)
    {
        var response = await _calendarQueryService.GetCalendarAsync(_currentUser.InternalUserId, month, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/details</summary>
    [HttpGet("active/details")]
    [ProducesResponseType(typeof(PlanDetailsResponse), 200)]
    public async Task<IActionResult> GetActivePlanDetails(CancellationToken ct)
    {
        var response = await _managementService.GetActivePlanDetailsAsync(_currentUser.InternalUserId, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/{planId}/cancel</summary>
    [HttpPost("{planId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelPlanResponse), 200)]
    public async Task<IActionResult> CancelPlan(Guid planId, [FromBody] CancelPlanRequest request, CancellationToken ct)
    {
        var response = await _managementService.CancelPlanAsync(_currentUser.InternalUserId, planId, request, ct);
        return Ok(response);
    }

    /// <summary>
    /// POST /api/v1/plans/active/long-horizon/activate-next-window — Phase
    /// 4L.4A. Explicitly activates the next server-owned rolling window for
    /// the authenticated user's active RollingLongHorizon plan once the
    /// current window is durably terminal. Never activates from a GET, from
    /// completion/not-today, or automatically in the background.
    /// </summary>
    [HttpPost("active/long-horizon/activate-next-window")]
    [ProducesResponseType(typeof(LongHorizonActivateNextWindowResponse), 200)]
    public async Task<IActionResult> ActivateLongHorizonNextWindow(
        [FromBody] LongHorizonActivateNextWindowRequest request, CancellationToken ct)
    {
        var response = await _rollingActivationService.ActivateNextWindowAsync(_currentUser.InternalUserId, request, ct);
        return Ok(response);
    }

    /// <summary>
    /// POST /api/v1/plans/active/long-horizon/retry — Phase 4L.4B. Explicitly
    /// restores a Blocked boundary back to Pending for the authenticated
    /// user's active RollingLongHorizon plan. Never activates a window itself
    /// — a later, separate activate-next-window call is required.
    /// </summary>
    [HttpPost("active/long-horizon/retry")]
    [ProducesResponseType(typeof(LongHorizonRetryContinuationResponse), 200)]
    public async Task<IActionResult> RetryLongHorizonContinuation(
        [FromBody] LongHorizonRetryContinuationRequest request, CancellationToken ct)
    {
        var response = await _rollingRetryService.RetryAsync(_currentUser.InternalUserId, request, ct);
        return Ok(response);
    }
}
