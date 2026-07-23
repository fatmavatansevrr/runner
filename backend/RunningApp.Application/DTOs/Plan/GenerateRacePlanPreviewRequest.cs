using RunningApp.Domain.Enums;
using System.Text.Json.Serialization;

namespace RunningApp.Application.DTOs.Plan;

/// <summary>
/// Public transport DTO for <c>POST /api/v1/plans/generate-preview/race</c>.
/// Contains only race and shared fields — no habit/custom/legacy properties
/// exist on this type at all, so Swagger cannot show them for this
/// operation and no cross-flow field can ever be sent here by construction.
///
/// Required-member strategy matches the rest of this DTO family: C#
/// `required` for every field mandatory on every race request (enforced by
/// System.Text.Json at deserialization — omission throws regardless of the
/// property's CLR type, so a `0`/`DateOnly.MinValue`/enum-zero default can
/// never be mistaken for "omitted"). `race_name`/readiness/`recent_race`
/// stay nullable and optional.
///
/// No property carries a business-value default — no initializers, no
/// constructor-assigned sample data. A valid example payload lives in
/// <c>backend/RunningApp.Api/generate-preview-race.http</c> and in this
/// project's test suite only.
/// </summary>
public sealed class GenerateRacePlanPreviewRequest
{
    public required GoalDistance GoalDistance { get; set; }

    /// <summary>See <see cref="GeneratePreviewRequest.Level"/> for why the property-level converter is required.</summary>
    [JsonConverter(typeof(RunningBackgroundCanonicalJsonConverter))]
    public required RunningBackground Level { get; set; }

    public required int DaysPerWeek { get; set; }
    public required DistanceUnit Unit { get; set; }
    public required DateOnly StartDate { get; set; }

    /// <summary>Canonical weekday tokens (mon..sun), distinct, count must equal <see cref="DaysPerWeek"/>.</summary>
    public required IReadOnlyList<Weekday> PreferredDays { get; set; }

    /// <summary>Required; must be a member of <see cref="PreferredDays"/>.</summary>
    public required Weekday LongRunDay { get; set; }

    public required DateOnly RaceDate { get; set; }

    /// <summary>
    /// Required and positive. The backend never invents or defaults this
    /// value — the caller resolves either a custom target time or a "go
    /// with average" choice into a concrete positive value, and tags which
    /// one via <see cref="TargetFinishTimeSource"/>, before calling this
    /// endpoint.
    /// </summary>
    public required int TargetFinishTimeSeconds { get; set; }

    /// <summary>
    /// Required. When "product_average", <see cref="TargetFinishTimeSeconds"/>
    /// must exactly equal the canonical average for <see cref="GoalDistance"/>
    /// (see <c>CanonicalTargetFinishTimePolicy</c>) or the request is
    /// rejected with 400 — a mismatched source/value pair is a contract
    /// error, not a feasibility question.
    /// </summary>
    public required TargetFinishTimeSource TargetFinishTimeSource { get; set; }

    public string? RaceName { get; set; }

    /// <summary>User-reported longest run in the last ~30 days, in km. `0` is explicit and distinct from omitted/null.</summary>
    public double? RecentLongestRunKm { get; set; }

    /// <summary>User-reported recent typical weekly running volume, in km. `0` is explicit and distinct from omitted/null.</summary>
    public double? RecentWeeklyVolumeKm { get; set; }

    /// <summary>User-reported recent typical runs per week. `0` is explicit and distinct from omitted/null.</summary>
    public int? RecentRunsPerWeek { get; set; }

    /// <summary>Optional previously-completed race result, distinct from <see cref="RaceName"/>/<see cref="RaceDate"/>/<see cref="TargetFinishTimeSeconds"/>.</summary>
    public RecentRaceInput? RecentRace { get; set; }
}
