using RunningApp.Domain.Enums;
using System;
using System.Text.Json.Serialization;

namespace RunningApp.Application.DTOs.Plan;

/// <summary>
/// INTERNAL-ONLY shape as of the generate-preview flow-specific contract
/// refactor: no controller action binds this type from JSON anymore (see
/// <c>GenerateRacePlanPreviewRequest</c>/<c>GenerateHabitPlanPreviewRequest</c>,
/// the current public transport DTOs). This type now exists purely as the
/// shared internal input to <see cref="RunningApp.Application.Services.PlanServices.GeneratePreviewAsync"/>
/// and the catalog/legacy generation pipeline beneath it — always
/// constructed by <see cref="RunningApp.Application.Commands.Plan.GeneratePreviewCommandMapper.ToInternalRequest"/>
/// from an already-validated <c>PlanPreviewCommand</c>, never from raw HTTP
/// input. Kept unchanged in shape (deliberately, to avoid rewriting the
/// entire catalog/resolver/legacy-engine pipeline for no behavioral gain)
/// except for the addition of <see cref="TargetFinishTimeSource"/> below.
///
/// Required-member strategy: every field that is genuinely mandatory for
/// EVERY request (both Race and Habit) uses C#'s <c>required</c> modifier.
/// System.Text.Json enforces <c>required</c> at deserialization time — an
/// omitted JSON property throws <see cref="System.Text.Json.JsonException"/>
/// regardless of the property's CLR type, which ASP.NET Core's input
/// formatter turns into an automatic HTTP 400 with structured errors. This
/// is deliberately NOT the same thing as checking a value-type property
/// against its CLR default (e.g. <c>DaysPerWeek == 0</c> or
/// <c>StartDate == default</c>) — that check cannot distinguish "omitted"
/// from "explicitly sent the zero/default value", which is exactly the
/// ambiguity this DTO's readiness fields (see below) must NOT have. Fields
/// that are conditionally required (race-only: RaceDate, LongRunDay,
/// TargetFinishTimeSeconds; optional for everyone: RecentRace, readiness)
/// stay nullable and are enforced by <see cref="RunningApp.Application.Validation.GeneratePreviewRequestValidator"/>,
/// which also encodes the race/habit consistency rules a type system alone
/// cannot express on a single transport shape.
///
/// This DTO carries NO business-value defaults anywhere — no property
/// initializers, no constructor-assigned sample data. Every value either
/// comes from the caller's JSON or is genuinely absent (null). A valid
/// example payload lives in <c>backend/RunningApp.Api/generate-preview.http</c>
/// and in this project's test suite — never in this class or in the Swagger
/// schema filter.
/// </summary>
public class GeneratePreviewRequest
{
    public required GoalType GoalType { get; set; }
    public required GoalDistance GoalDistance { get; set; }

    /// <summary>
    /// Running Background V2.1 — the public request boundary. A
    /// property-level <c>[JsonConverter]</c> is required here (not just the
    /// type-level attribute on <see cref="RunningBackground"/>) because
    /// System.Text.Json gives a converter registered in
    /// <c>JsonSerializerOptions.Converters</c> (the API's global
    /// <c>JsonStringEnumConverter(SnakeCaseLower)</c>, see
    /// <c>RunningApp.Api/Program.cs</c>) higher precedence than a type-level
    /// attribute converter. <see cref="RunningBackgroundCanonicalJsonConverter"/>
    /// accepts ONLY the four canonical values — legacy aliases
    /// ("new_to_running", "used_to_run", "running_regularly") are rejected
    /// here with a typed 400 validation error; they are not current product
    /// options and this is the current public API contract. Do not switch
    /// this to <c>RunningBackgroundJsonConverter</c> (that type is
    /// historical-snapshot-read-only — see its own docs).
    /// </summary>
    [JsonConverter(typeof(RunningBackgroundCanonicalJsonConverter))]
    public required RunningBackground Level { get; set; }
    public required int DaysPerWeek { get; set; }
    public required DistanceUnit Unit { get; set; }

    /// <summary>Race-only. Must be null for Habit requests (validator-enforced).</summary>
    public string? RaceName { get; set; }

    /// <summary>Required for Race, must be null for Habit (validator-enforced).</summary>
    public DateOnly? RaceDate { get; set; }

    /// <summary>
    /// Required and positive for Race, must be null for Habit
    /// (validator-enforced). The backend never invents or defaults this
    /// value — the caller must resolve either a custom target time or a
    /// "go with average" choice into a concrete positive value before
    /// calling this endpoint.
    /// </summary>
    public int? TargetFinishTimeSeconds { get; set; }

    /// <summary>
    /// How <see cref="TargetFinishTimeSeconds"/> was derived. Null for Habit
    /// requests (which never carry a target time at all) and for any
    /// internal caller that predates this field. See
    /// PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md
    /// for how <see cref="RunningApp.Application.RuntimeCatalog.Resolvers.GoalFeasibilityResolver"/>
    /// uses this.
    /// </summary>
    public TargetFinishTimeSource? TargetFinishTimeSource { get; set; }

    /// <summary>
    /// Required for every plan. The first day of the plan's first 7-day
    /// window — it does not need to be a Monday (see
    /// <see cref="RunningApp.Application.Common.WeekdayCsv"/> and
    /// <c>CatalogWeekSkeletonCalendarMaterializer.MapWeekdayToDateInWeek</c>,
    /// both of which map weekdays into a week's own [StartDate, StartDate+6]
    /// range regardless of which weekday StartDate itself falls on).
    /// </summary>
    public required DateOnly StartDate { get; set; }

    /// <summary>
    /// Required for Race and Habit plans. Canonical weekday tokens
    /// (mon..sun), distinct, count must equal <see cref="DaysPerWeek"/>.
    /// </summary>
    public required IReadOnlyList<Weekday> PreferredDays { get; set; }

    public int? WeeklyAvailability { get; set; }     // hours per week available
    public double? PreferredPace { get; set; }       // min/km comfortable pace

    /// <summary>
    /// Required for Race plans; optional for Habit plans. If provided, must
    /// be a member of <see cref="PreferredDays"/>.
    /// </summary>
    public Weekday? LongRunDay { get; set; }
    public string? HabitPlanType { get; set; }
    public string? CustomGoalType { get; set; }
    public int? CustomDurationWeeks { get; set; }
    public int? CustomTargetTimeSeconds { get; set; }

    // ── Backend Integration Phase 4B: runtime fitness-evidence input contract ──
    // All nullable/optional. `null` means "not provided/unknown"; an explicit
    // `0` is a distinct, meaningful value and must never be coerced to null
    // or defaulted away anywhere in validation or downstream mapping.

    /// <summary>User-reported longest run in the last ~30 days, in km.</summary>
    public double? RecentLongestRunKm { get; set; }

    /// <summary>User-reported recent typical weekly running volume, in km.</summary>
    public double? RecentWeeklyVolumeKm { get; set; }

    /// <summary>User-reported recent typical runs per week.</summary>
    public int? RecentRunsPerWeek { get; set; }

    /// <summary>
    /// Optional previously-completed race result, distinct from the target
    /// race (<see cref="RaceName"/>/<see cref="RaceDate"/>/<see cref="TargetFinishTimeSeconds"/>).
    /// </summary>
    public RecentRaceInput? RecentRace { get; set; }
}
