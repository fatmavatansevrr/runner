using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.TargetDistanceProjection;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.Commands.Plan;

/// <summary>
/// Maps between the public, flow-specific transport DTOs
/// (<see cref="GenerateRacePlanPreviewRequest"/>/<see cref="GenerateHabitPlanPreviewRequest"/>),
/// their typed <see cref="PlanPreviewCommand"/>, and the internal
/// (no-longer-public) <see cref="GeneratePreviewRequest"/> shape the
/// existing catalog/legacy generation pipeline still consumes.
///
/// Callers of <see cref="ToCommand(GenerateRacePlanPreviewRequest)"/>/
/// <see cref="ToCommand(GenerateHabitPlanPreviewRequest)"/> MUST have already
/// run the matching flow-specific validator
/// (<c>GenerateRacePlanPreviewRequestValidator</c>/<c>GenerateHabitPlanPreviewRequestValidator</c>).
/// <see cref="ToCommand(GeneratePreviewRequest)"/> is used only by
/// <c>PlanServices.ConfirmPlanAsync</c>, re-deriving a command from an
/// already-validated persisted preview snapshot — the null-forgiving
/// assertions there are the one place this codebase allows them, per this
/// class's original design.
/// </summary>
public static class GeneratePreviewCommandMapper
{
    public static RacePlanPreviewCommand ToCommand(GenerateRacePlanPreviewRequest request) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = request.GoalDistance,
        Level = request.Level,
        DaysPerWeek = request.DaysPerWeek,
        Unit = request.Unit,
        StartDate = request.StartDate,
        PreferredDays = request.PreferredDays,
        RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
        RecentLongestRunKm = request.RecentLongestRunKm,
        RecentRunsPerWeek = request.RecentRunsPerWeek,
        RecentRace = request.RecentRace,
        RaceDate = request.RaceDate,
        LongRunDay = request.LongRunDay,
        TargetFinishTimeSeconds = request.TargetFinishTimeSeconds,
        TargetFinishTimeSource = request.TargetFinishTimeSource,
        RaceName = request.RaceName,
        TargetDistanceKm = request.TargetDistanceKm,
    };

    public static HabitPlanPreviewCommand ToCommand(GenerateHabitPlanPreviewRequest request) => new()
    {
        GoalType = GoalType.Habit,
        GoalDistance = request.GoalDistance,
        Level = request.Level,
        DaysPerWeek = request.DaysPerWeek,
        Unit = request.Unit,
        StartDate = request.StartDate,
        PreferredDays = request.PreferredDays,
        LongRunDay = request.LongRunDay,
    };

    /// <summary>
    /// Builds the internal (no-longer-public) <see cref="GeneratePreviewRequest"/>
    /// shape from an already-validated command, so the existing catalog/legacy
    /// generation pipeline can keep consuming it unchanged. The only place
    /// this direction of mapping happens.
    /// </summary>
    public static GeneratePreviewRequest ToInternalRequest(PlanPreviewCommand command)
    {
        var request = new GeneratePreviewRequest
        {
            GoalType = command.GoalType,
            GoalDistance = command.GoalDistance,
            Level = command.Level,
            DaysPerWeek = command.DaysPerWeek,
            Unit = command.Unit,
            StartDate = command.StartDate,
            PreferredDays = command.PreferredDays,
            RecentWeeklyVolumeKm = command.RecentWeeklyVolumeKm,
            RecentLongestRunKm = command.RecentLongestRunKm,
            RecentRunsPerWeek = command.RecentRunsPerWeek,
            RecentRace = command.RecentRace,
        };

        if (command is RacePlanPreviewCommand raceCommand)
        {
            request.RaceDate = raceCommand.RaceDate;
            request.LongRunDay = raceCommand.LongRunDay;
            request.TargetFinishTimeSeconds = raceCommand.TargetFinishTimeSeconds;
            request.TargetFinishTimeSource = raceCommand.TargetFinishTimeSource;
            request.RaceName = raceCommand.RaceName;

            // ── PHASE DIST-GEN.3 — the one canonicalization/eligibility gate ──
            // Only reachable when GoalDistance == Custom AND the validator
            // already accepted a well-formed TargetDistanceKm (see
            // GenerateRacePlanPreviewRequestValidator). Resolves the numeric
            // target to a catalog family, checks it against the single
            // approved dark pilot triple (Dark16KPilotEligibilityPolicy), and
            // — only if eligible — substitutes GoalDistance/TargetDistanceKmOverride
            // to exactly mirror the internal shape the dark pilot's own test
            // harness has always used. Any other Custom target (out of every
            // band, or in-band but not the exact eligible triple) is rejected
            // here with UnsupportedTargetDistanceException (400), never
            // silently substituted into an unrelated canonical family.
            if (raceCommand.GoalDistance == GoalDistance.Custom && raceCommand.TargetDistanceKm is { } targetDistanceKm)
            {
                var resolution = new CanonicalDistanceFamilyResolver().Resolve(targetDistanceKm);

                var eligible = Dark16KPilotEligibilityPolicy.IsEligible(
                    targetDistanceKm, resolution.CanonicalDistanceFamily, raceCommand.Level, raceCommand.DaysPerWeek);

                if (!eligible)
                {
                    throw new UnsupportedTargetDistanceException(
                        $"target_distance_km {targetDistanceKm} km (resolved family: {resolution.CanonicalDistanceFamily}, " +
                        $"level: {raceCommand.Level}, days_per_week: {raceCommand.DaysPerWeek}) is not an approved " +
                        "public target-distance projection. Only the frozen pilot triple " +
                        $"(target_distance_km={Dark16KPilotEligibilityPolicy.EligibleTargetDistanceKm}, " +
                        $"resolved family={Dark16KPilotEligibilityPolicy.EligibleParentDistanceFamily}, " +
                        $"level={Dark16KPilotEligibilityPolicy.EligibleLevel}, " +
                        $"days_per_week={Dark16KPilotEligibilityPolicy.EligibleRunsPerWeek}) is currently supported. " +
                        "Reason: UNSUPPORTED_TARGET_DISTANCE.");
                }

                request.GoalDistance = resolution.CanonicalDistanceFamily;
                request.TargetDistanceKmOverride = targetDistanceKm;
            }
        }
        else if (command is HabitPlanPreviewCommand habitCommand)
        {
            request.LongRunDay = habitCommand.LongRunDay;
        }

        return request;
    }

    /// <summary>
    /// Maps a validated internal <see cref="GeneratePreviewRequest"/> onto
    /// its typed <see cref="PlanPreviewCommand"/> — used only by
    /// <c>PlanServices.ConfirmPlanAsync</c> when re-deriving a command from
    /// an already-validated persisted preview snapshot.
    /// </summary>
    public static PlanPreviewCommand ToCommand(GeneratePreviewRequest request)
    {
        if (request.GoalType == GoalType.Race)
        {
            return new RacePlanPreviewCommand
            {
                GoalType = request.GoalType,
                GoalDistance = request.GoalDistance,
                Level = request.Level,
                DaysPerWeek = request.DaysPerWeek,
                Unit = request.Unit,
                StartDate = request.StartDate,
                PreferredDays = request.PreferredDays,
                RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
                RecentLongestRunKm = request.RecentLongestRunKm,
                RecentRunsPerWeek = request.RecentRunsPerWeek,
                RecentRace = request.RecentRace,
                RaceDate = request.RaceDate!.Value,
                LongRunDay = request.LongRunDay!.Value,
                TargetFinishTimeSeconds = request.TargetFinishTimeSeconds!.Value,
                TargetFinishTimeSource = request.TargetFinishTimeSource ?? TargetFinishTimeSource.UserDefined,
                RaceName = request.RaceName,
            };
        }

        return new HabitPlanPreviewCommand
        {
            GoalType = request.GoalType,
            GoalDistance = request.GoalDistance,
            Level = request.Level,
            DaysPerWeek = request.DaysPerWeek,
            Unit = request.Unit,
            StartDate = request.StartDate,
            PreferredDays = request.PreferredDays,
            RecentWeeklyVolumeKm = request.RecentWeeklyVolumeKm,
            RecentLongestRunKm = request.RecentLongestRunKm,
            RecentRunsPerWeek = request.RecentRunsPerWeek,
            RecentRace = request.RecentRace,
            LongRunDay = request.LongRunDay,
        };
    }
}
