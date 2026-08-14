using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWorkoutBinding;

/// <summary>
/// Backend Integration Phase 4G.6A.4B (family rule generalized in Phase
/// 4G.6A.4C) — a separate, narrow, catalog-aware validator for the workout
/// references a <see cref="PreparationRunwayBlockWorkoutBindingEngine"/>
/// binding selected. Deliberately kept OUT of the pure, generic binder
/// engine (which never touches a file): this class is the only piece of
/// the binder feature that reads catalog content, and it does so by
/// reusing the existing, already-established
/// <see cref="ICatalogWorkoutDefinitionLoader"/> (Phase 4F.6B) rather than
/// a parallel reader. Dark: not registered in DI, not invoked by any
/// orchestrator.
///
/// Family rule: any non-race-day family (EASY, LONG_RUN, QUALITY) is
/// accepted -- Preparation Runway blocks legitimately span multiple
/// families (CONSISTENCY/GENERAL_ENDURANCE/PRE_SPECIFIC_TRANSITION use
/// EASY/LONG_RUN content; AEROBIC_STRENGTH uses QUALITY content). Only the
/// RACE family (reserved for actual race-day content) is excluded.
/// PREPARATION_RUNWAY eligibility is the sole gate on eligiblePhases --
/// membership in RACE_SPECIFIC (a Core-phase scheduling marker on
/// general-purpose workouts like EASY_STANDARD, unrelated to whether the
/// workout's own content is race-specific) is never itself disqualifying.
/// </summary>
internal static class PreparationRunwayBlockWorkoutReferenceValidator
{
    private const string RunwayEligiblePhase = "PREPARATION_RUNWAY";
    private static readonly string[] AllowedFamilies = ["EASY", "LONG_RUN", "QUALITY"];
    private static readonly string[] ForbiddenIntensityTokens = ["THRESHOLD", "MAXIMAL", "ALL_OUT", "SPRINT", "VO2MAX", "GOAL_PACE", "TARGET_TIME"];

    public static async Task<PreparationRunwayWorkoutBindingFailureCode?> ValidateAsync(
        ICatalogWorkoutDefinitionLoader loader,
        PreparationRunwayWorkoutReference reference,
        CancellationToken ct = default)
    {
        CatalogWorkoutDefinitionSummary summary;
        try
        {
            summary = await loader.LoadAsync(new RuntimeCatalog.PlanCatalogReference(reference.WorkoutId, reference.WorkoutVersion), ct);
        }
        catch (PlanCatalogLoadException)
        {
            return PreparationRunwayWorkoutBindingFailureCode.WorkoutReferenceNotFound;
        }

        if (summary.Version != reference.WorkoutVersion)
        {
            return PreparationRunwayWorkoutBindingFailureCode.WorkoutVersionNotFound;
        }

        if (!summary.EligiblePhases.Contains(RunwayEligiblePhase))
        {
            return PreparationRunwayWorkoutBindingFailureCode.WorkoutNotRunwayEligible;
        }

        if (!AllowedFamilies.Contains(summary.Family))
        {
            return PreparationRunwayWorkoutBindingFailureCode.WorkoutSemanticMismatch;
        }

        var forbiddenFound = summary.Components.Any(c =>
            ForbiddenIntensityTokens.Any(token => c.IntensityDescriptor.Contains(token, StringComparison.Ordinal)));
        if (forbiddenFound)
        {
            return PreparationRunwayWorkoutBindingFailureCode.WorkoutSemanticMismatch;
        }

        return null;
    }
}
