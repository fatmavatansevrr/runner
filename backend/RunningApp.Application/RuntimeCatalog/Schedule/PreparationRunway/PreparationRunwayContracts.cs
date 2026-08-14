using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;

internal enum PreparationRunwayBlockType { Consistency, GeneralEndurance, AerobicStrength, PreSpecificTransition }
internal enum PreparationRunwayPrescriptionProfile { Standard, Light }
internal enum PreparationRunwayPlanningStatus { Planned, DecisionRequired, Unsupported, NotApplicable, InvalidInput }
internal enum PreparationRunwayNeedLevel { NotEvaluated, Low, Moderate, High }
internal enum PreparationRunwayExperienceVocabulary { PlanCatalogRunningExperience, BackendRunningBackground }
internal enum RacePlanCompositionType { StandaloneCore, PreparationRunwayPlusCore, ReadinessOnly, CompressedCore }

internal enum PreparationRunwayPlanningReason
{
    ExperienceMappingUnresolved,
    AerobicStrengthLightSemanticsUnresolved,
    ShortRunwayRouteSelectionUnresolved,
    BlockCountRouteConflict,
    LongRunwayCapacityExceeded,
    LongRunwayContinuationPolicyMissing,
    ReadinessRoutingUnresolved,
    InsufficientEffectiveRunway,
    InvalidDateRange,
    InvalidDerivedDuration,
    InvalidBlockAllocation
}

/// <summary>A vocabulary-qualified experience value. It deliberately performs no New-to-Beginner mapping.</summary>
internal sealed record PreparationRunwayExperienceReference(
    PreparationRunwayExperienceVocabulary Vocabulary,
    string Value);

/// <summary>Observed need states only. No score, inference, route, or block ordering is computed here.</summary>
internal sealed record PreparationNeedProfile(
    PreparationRunwayNeedLevel ConsistencyNeed,
    PreparationRunwayNeedLevel FrequencyReadiness,
    PreparationRunwayNeedLevel VolumeReadiness,
    PreparationRunwayNeedLevel LongRunReadiness,
    PreparationRunwayNeedLevel QualityReadiness,
    PreparationRunwayNeedLevel CoreEntryReadiness);

/// <summary>Race-anchored planning inputs and their already-derived duration facts.</summary>
internal sealed record PreparationRunwayContext(
    GoalDistance Distance,
    PreparationRunwayExperienceReference Experience,
    PreparationNeedProfile NeedProfile,
    decimal? RecentWeeklyVolumeKm,
    decimal? RecentLongestRunKm,
    int? RecentRunsPerWeek,
    int TargetRunsPerWeek,
    DateOnly StartDate,
    DateOnly CoreStartDate,
    DateOnly RaceDate,
    int RunwayDays,
    int FullRunwayWeeks,
    int LeadingPartialDays,
    int PreferredCoreWeeks,
    RacePlanCompositionType CompositionType);

/// <summary>One full-week-only runway allocation entry. No partial days are representable here.</summary>
internal sealed record PreparationRunwayBlockAllocation(
    PreparationRunwayBlockType BlockType,
    PreparationRunwayPrescriptionProfile PrescriptionProfile,
    int FullWeekCount,
    int SequenceIndex,
    int? MinimumWeeks = null,
    int? MaximumWeeks = null,
    string? SourceRuleId = null,
    IReadOnlyList<string>? DecisionTrace = null);

/// <summary>Full-week runway allocation facts. Leading partial days never appear as a block.</summary>
internal sealed record PreparationRunwayAllocation(
    int RunwayDays,
    int FullRunwayWeeks,
    int LeadingPartialDays,
    IReadOnlyList<PreparationRunwayBlockAllocation> Blocks);

/// <summary>Materialization-boundary metadata, separate from the runway block taxonomy.</summary>
internal sealed record PreparationRunwayLeadingPartialSpan(
    DateOnly StartDate,
    DateOnly EndDate,
    int DayCount,
    PreparationRunwayBlockType InheritedBlockType,
    PreparationRunwayPrescriptionProfile InheritedPrescriptionProfile,
    bool AllowsZeroSessions,
    bool AllowsQualityProgression);

/// <summary>Composition facts only; it does not change the live horizon policy.</summary>
internal sealed record RacePlanCompositionMetadata(
    RacePlanCompositionType CompositionType,
    int TotalAvailableDays,
    int PreferredCoreWeeks,
    int PreferredCoreDays,
    DateOnly StartDate,
    DateOnly CoreStartDate,
    DateOnly RaceDate,
    int RunwayDays,
    int FullRunwayWeeks,
    int LeadingPartialDays,
    PreparationRunwayLeadingPartialSpan? LeadingPartialSpan);

internal sealed record PreparationRunwayPlanningFinding(
    PreparationRunwayPlanningReason Reason,
    string? Detail = null);

/// <summary>Represents success and every known non-success outcome without normalizing or inventing a decision.</summary>
internal sealed record PreparationRunwayPlanningResult(
    PreparationRunwayPlanningStatus Status,
    RacePlanCompositionMetadata Composition,
    PreparationRunwayAllocation? Allocation,
    IReadOnlyList<PreparationRunwayPlanningFinding> Findings);

internal sealed record PreparationRunwayValidationResult(
    bool IsValid,
    IReadOnlyList<PreparationRunwayPlanningFinding> Findings)
{
    public static PreparationRunwayValidationResult Valid() => new(true, []);
    public static PreparationRunwayValidationResult Invalid(IEnumerable<PreparationRunwayPlanningFinding> findings) =>
        new(false, findings.ToArray());
}

/// <summary>
/// Phase 4G.6A.1 — generic day-count derivation for Preparation Runway date
/// fields, consuming already-resolved canonical exclusive-day horizon values
/// (AvailableFullWeeks, LeadingPartialDays, PreferredCoreWeeks -- the same
/// three values the live horizon-classification authority already exposes)
/// instead of independently subtracting StartDate/RaceDate to derive a
/// runway duration. Deliberately takes no RaceDate parameter at all: RaceDate
/// is never read here, so this type cannot reintroduce the prior inclusive
/// race-anchored arithmetic even by accident.
///
/// This is pure, generic arithmetic: it contains no knowledge of any
/// specific week-count boundary (15/20/21/52/53 or any other), no
/// composition-eligibility judgment, and no block/route/allocation logic --
/// those remain out of scope for this contract, per this phase's own
/// restriction. Callers (currently: tests only) remain fully responsible
/// for supplying an already-resolved PreferredCoreWeeks value (e.g. a
/// candidate's own CoreCycle.DefaultWeeks) -- this type never hard-codes a
/// core-duration constant.
/// </summary>
internal static class PreparationRunwayDateAuthority
{
    /// <summary>
    /// Derives RunwayFullWeeks/RunwayPartialDays/RunwayDays/CoreStartDate
    /// from already-resolved canonical exclusive-day horizon values.
    /// RunwayFullWeeks = AvailableFullWeeks - PreferredCoreWeeks;
    /// RunwayPartialDays = LeadingPartialDays (the partial remainder sits
    /// before the runway begins, so it passes through unchanged).
    /// CoreStartDate is placed by adding the derived RunwayDays to
    /// <paramref name="startDate"/> -- never by subtracting from a race
    /// date.
    /// </summary>
    public static PreparationRunwayDateAuthorityResult Derive(
        DateOnly startDate, int availableFullWeeks, int leadingPartialDays, int preferredCoreWeeks)
    {
        var runwayFullWeeks = availableFullWeeks - preferredCoreWeeks;
        var runwayPartialDays = leadingPartialDays;
        var runwayDays = (runwayFullWeeks * 7) + runwayPartialDays;
        var coreStartDate = startDate.AddDays(runwayDays);
        return new PreparationRunwayDateAuthorityResult(runwayFullWeeks, runwayPartialDays, runwayDays, coreStartDate);
    }
}

/// <summary>Generic derivation output — see <see cref="PreparationRunwayDateAuthority.Derive"/>.</summary>
internal sealed record PreparationRunwayDateAuthorityResult(
    int RunwayFullWeeks,
    int RunwayPartialDays,
    int RunwayDays,
    DateOnly CoreStartDate);
