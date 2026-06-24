using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.Home;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Services;
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
    private readonly IPlanManagementService _managementService;
    private readonly IHomeQueryService _homeQueryService;
    private readonly ICalendarQueryService _calendarQueryService;

    private const string MockUserId = "mock-user-001";

    public PlansController(
        IPlanPreviewService previewService,
        IPlanConfirmationService confirmationService,
        IPlanManagementService managementService,
        IHomeQueryService homeQueryService,
        ICalendarQueryService calendarQueryService)
    {
        _previewService = previewService;
        _confirmationService = confirmationService;
        _managementService = managementService;
        _homeQueryService = homeQueryService;
        _calendarQueryService = calendarQueryService;
    }

    /// <summary>POST /api/v1/plans/generate-preview — returns a seed template preview</summary>
    [HttpPost("generate-preview")]
    [ProducesResponseType(typeof(GeneratePreviewResponse), 200)]
    public async Task<IActionResult> GeneratePreview([FromBody] GeneratePreviewRequest request, CancellationToken ct)
    {
        var response = await _previewService.GeneratePreviewAsync(MockUserId, request, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/confirm — persists the confirmed plan</summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmPlanResponse), 200)]
    public async Task<IActionResult> ConfirmPlan([FromBody] ConfirmPlanRequest request, CancellationToken ct)
    {
        var response = await _confirmationService.ConfirmPlanAsync(MockUserId, request, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/home</summary>
    [HttpGet("active/home")]
    [ProducesResponseType(typeof(HomeResponse), 200)]
    public async Task<IActionResult> GetHome(CancellationToken ct)
    {
        var response = await _homeQueryService.GetHomeAsync(MockUserId, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/calendar?month=YYYY-MM</summary>
    [HttpGet("active/calendar")]
    [ProducesResponseType(typeof(System.Collections.Generic.List<TrainingDayResponse>), 200)]
    public async Task<IActionResult> GetCalendar([FromQuery] string month, CancellationToken ct)
    {
        var response = await _calendarQueryService.GetCalendarAsync(MockUserId, month, ct);
        return Ok(response);
    }

    /// <summary>GET /api/v1/plans/active/details</summary>
    [HttpGet("active/details")]
    public async Task<IActionResult> GetActivePlanDetails(CancellationToken ct)
    {
        var response = await _managementService.GetActivePlanDetailsAsync(MockUserId, ct);
        return Ok(response);
    }

    /// <summary>POST /api/v1/plans/{planId}/cancel</summary>
    [HttpPost("{planId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelPlanResponse), 200)]
    public async Task<IActionResult> CancelPlan(Guid planId, [FromBody] CancelPlanRequest request, CancellationToken ct)
    {
        var response = await _managementService.CancelPlanAsync(MockUserId, planId, request, ct);
        return Ok(response);
    }
}
