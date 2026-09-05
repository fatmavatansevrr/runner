using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal enum LongHorizonRollingInitialActivationStatus
{
    Approved,
    Blocked,
}

internal enum LongHorizonRollingInitialActivationFailureReason
{
    InvalidStructuralHorizon,
    InvalidEligibility,
    InvalidOnboardingEvidence,
    CatalogResolutionFailure,
    GeneralEnduranceNumericInfeasibility,
    SessionAllocationInfeasibility,
    CalendarAssignmentFailure,
    ActivatedWeekContractFailure,
    ActivationWindowAtomicityFailure,
    SafetyOrFeasibilityFailure,
    FinalResultValidationFailure,
}

internal enum LongHorizonRollingInitialActivationValidationStage
{
    InputAndEligibility,
    StructuralRoadmap,
    InitialActivationContext,
    ActivationWindow,
    GeneralEnduranceNumericMaterialization,
    ActivatedNumericWeeks,
    Atomicity,
    FinalResult,
}

internal sealed record LongHorizonRollingInitialActivationFailure
{
    public required LongHorizonRollingInitialActivationFailureReason Reason { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string OriginalExceptionType { get; init; }
}

internal sealed record LongHorizonRollingInitialActivationRequest
{
    public required LongHorizonCompositionDecision CompositionDecision { get; init; }
    public required GoalType GoalType { get; init; }
    public required GoalDistance GoalDistance { get; init; }
    public required RunningBackground Level { get; init; }
    public required int DaysPerWeek { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly RaceDate { get; init; }
    public required LongHorizonGeEntryBaselineInput OnboardingBaseline { get; init; }
    public required IReadOnlyList<DayOfWeek> PreferredDays { get; init; }
    public required DayOfWeek LongRunDay { get; init; }
    public required string CatalogRoot { get; init; }
    public required ICatalogWorkoutDefinitionLoader WorkoutLoader { get; init; }
    public LongHorizonEvidenceAuthorityRecord? PaceIntensityContext { get; init; }
}

internal sealed record LongHorizonRollingInitialActivationValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<LongHorizonRollingInitialActivationValidationStage> CompletedStages { get; init; }
}

internal sealed record LongHorizonRollingInitialActivationResult
{
    public required LongHorizonRollingInitialActivationStatus Status { get; init; }
    public LongHorizonStructuralRoadmap? StructuralRoadmap { get; init; }
    public LongHorizonGeneratedStructuralSkeleton? StructuralSkeleton { get; init; }
    public LongHorizonInitialActivationContext? InitialActivationContext { get; init; }
    public RollingNumericActivationWindow? ActivationWindow { get; init; }
    public required IReadOnlyList<ActivatedNumericWeek> ActivatedNumericWeeks { get; init; }
    public required IReadOnlyList<ActivatedNumericWeek> PendingNumericWeeks { get; init; }
    public LongHorizonContextVersion? ContextVersion { get; init; }
    public required LongHorizonRollingInitialActivationValidationResult Validation { get; init; }
    public required IReadOnlyList<string> Provenance { get; init; }
    public LongHorizonRollingInitialActivationFailure? Failure { get; init; }
}

internal static class LongHorizonRollingInitialActivationInputValidator
{
    public static void Validate(LongHorizonRollingInitialActivationRequest request)
    {
        var decision = request.CompositionDecision;
        if (decision.AvailableFullWeeks is < 21 or > 52
            || decision.HorizonPath != LongHorizonPath.LongHorizonGeneralEnduranceRunwayAndCore
            || decision.Eligibility != LongHorizonEligibility.SupportedLongHorizon
            || decision.GeneralEnduranceWeeks != decision.AvailableFullWeeks - 20
            || decision.PreparationRunwayWeeks != 8
            || decision.CoreWeeks != 12
            || decision.ReadinessProfile is null)
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.InvalidStructuralHorizon,
                "LONG_HORIZON_ROLLING_INITIAL_STRUCTURAL_HORIZON_INVALID",
                "Rolling initial activation accepts only the existing eligible 21-52 week GE + 8-week Runway + 12-week Core decision.");
        }

        // Phase 10K-FREQ.6D.26 -- widened from 4-or-5 to include 6 (Intermediate
        // x6D, approved FREQ.6D.23/6D.25). Phase 10K-GEN.9 -- widened further
        // to admit Advanced 3D/4D/5D/6D (GEN.7/GEN.8 authority). This is the
        // internal/dark runtime eligibility gate, distinct from the public
        // routing gate (V1CatalogPilotIdentityPolicy), which is intentionally
        // left untouched -- the public gate remains closed for every Advanced
        // frequency.
        //
        // Phase 10K-GEN.32 -- NOT widened to admit 2D this phase. GEN.32's own
        // investigation (PHASE_10K_GEN_32 §3.3) found that opening this gate
        // alone would let 2D traffic reach at least three unfixed downstream
        // defects (the Level-dispatch branch below/in ExistingLongHorizonGeWindowMaterializer
        // has no Beginner case and would silently apply Intermediate's numeric
        // policy to Beginner; LongHorizonStructuralValidator's per-skeleton
        // uniform expectedKey/expectedEasy shape does not yet recognize 2D's
        // per-week alternation; LongHorizonGeMaintenanceWindowMaterializer's
        // checkpoint-path Allocate call does not yet honor a week's own
        // HasKeySession flag) -- left closed deliberately rather than shipping
        // a gate that is open but not yet safe to exercise.
        var levelFrequencyEligible =
            (request.Level == RunningBackground.Intermediate && request.DaysPerWeek is 4 or 5 or 6) ||
            (request.Level == RunningBackground.Advanced && request.DaysPerWeek is 3 or 4 or 5 or 6);
        if (request.GoalType != GoalType.Race || request.GoalDistance != GoalDistance.TenK || !levelFrequencyEligible)
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.InvalidEligibility,
                "LONG_HORIZON_ROLLING_INITIAL_ELIGIBILITY_INVALID",
                "Rolling initial activation is restricted to Race / exact 10K / Intermediate 4-6 days per week / Advanced 3-6 days per week.");
        }

        if (request.StartDate >= request.RaceDate
            || request.RaceDate.DayNumber - request.StartDate.DayNumber < decision.AvailableFullWeeks * 7
            || request.RaceDate.DayNumber - request.StartDate.DayNumber >= (decision.AvailableFullWeeks + 1) * 7)
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.InvalidStructuralHorizon,
                "LONG_HORIZON_ROLLING_INITIAL_DATE_RANGE_INVALID",
                "StartDate/RaceDate must preserve the authoritative decision's full-week structural horizon; a leading partial remainder may not be reclassified here.");
        }

        if (request.OnboardingBaseline.RecentWeeklyVolumeKm is not (> 0)
            || request.OnboardingBaseline.RecentLongestRunKm is <= 0
            || request.OnboardingBaseline.RecentRunsPerWeek is <= 0)
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.InvalidOnboardingEvidence,
                "LONG_HORIZON_ROLLING_INITIAL_ONBOARDING_EVIDENCE_INVALID",
                "Onboarding weekly volume must be positive; optional longest-run and frequency evidence must be positive when supplied.");
        }

        if (request.PreferredDays.Count != request.DaysPerWeek || request.PreferredDays.Distinct().Count() != request.DaysPerWeek
            || !request.PreferredDays.Contains(request.LongRunDay))
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.CalendarAssignmentFailure,
                "LONG_HORIZON_ROLLING_INITIAL_CALENDAR_INPUT_INVALID",
                $"Exactly {request.DaysPerWeek} distinct preferred days including the long-run day are required.");
        }

        if (string.IsNullOrWhiteSpace(request.CatalogRoot))
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.CatalogResolutionFailure,
                "LONG_HORIZON_ROLLING_INITIAL_CATALOG_ROOT_INVALID",
                "A catalog root is required for the existing GE structural/workout binding authority.");
        }
    }
}

internal sealed class LongHorizonRollingInitialActivationException : LongHorizonRollingContractException
{
    public LongHorizonRollingInitialActivationFailureReason Reason { get; }

    public LongHorizonRollingInitialActivationException(
        LongHorizonRollingInitialActivationFailureReason reason,
        string code,
        string message,
        Exception? innerException = null)
        : base(code, innerException is null ? message : $"{message} Original failure: {innerException.GetType().Name}: {innerException.Message}")
    {
        Reason = reason;
    }
}

internal static class LongHorizonRollingInitialActivationResultValidator
{
    public static void Validate(LongHorizonRollingInitialActivationResult result)
    {
        if (result.Status == LongHorizonRollingInitialActivationStatus.Approved)
        {
            if (result.Failure is not null || result.StructuralRoadmap is null || result.StructuralSkeleton is null
                || result.InitialActivationContext is null || result.ActivationWindow is null || result.ContextVersion is null
                || result.ActivatedNumericWeeks.Count != result.ActivationWindow.ActualWindowSizeWeeks
                || result.PendingNumericWeeks.Count + result.ActivatedNumericWeeks.Count != result.StructuralRoadmap.TotalWeeks)
            {
                throw new LongHorizonRollingInitialActivationException(
                    LongHorizonRollingInitialActivationFailureReason.FinalResultValidationFailure,
                    "LONG_HORIZON_ROLLING_INITIAL_RESULT_INVALID",
                    "Approved initial activation must contain one complete roadmap, context, atomic window, activated prefix, and pending suffix.");
            }
        }
        else if (result.Failure is null || result.ActivatedNumericWeeks.Count != 0)
        {
            throw new LongHorizonRollingInitialActivationException(
                LongHorizonRollingInitialActivationFailureReason.FinalResultValidationFailure,
                "LONG_HORIZON_ROLLING_INITIAL_BLOCKED_RESULT_INVALID",
                "Blocked initial activation must contain exactly one typed failure and no activated weeks.");
        }

        foreach (var week in result.ActivatedNumericWeeks.Concat(result.PendingNumericWeeks))
        {
            LongHorizonActivatedNumericWeekValidator.Validate(week);
        }
    }
}
