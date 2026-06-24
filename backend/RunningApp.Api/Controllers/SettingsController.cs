using Microsoft.AspNetCore.Mvc;
using RunningApp.Application.DTOs.Settings;

namespace RunningApp.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
public class SettingsController : ControllerBase
{
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(SettingsPreferencesResponse), 200)]
    public IActionResult GetPreferences()
    {
        // Static Phase 1 settings response
        var response = new SettingsPreferencesResponse
        {
            ReminderStyle = "balanced",
            WorkoutRemindersEnabled = true,
            EveningReminderEnabled = true,
            ReminderTime = "08:00"
        };
        return Ok(response);
    }
}
