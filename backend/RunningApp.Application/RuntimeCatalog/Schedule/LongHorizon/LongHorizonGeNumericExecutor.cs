using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.6 — the input evidence for the first GE week's numeric baseline.
/// Mirrors the same evidence categories <c>CatalogVolumeAndLongRunPlanner</c>'s
/// own starting-volume/long-run-anchor decision already consumes
/// (<c>RecentWeeklyVolumeKm</c>/<c>RecentLongestRunKm</c>) -- this phase
/// reuses those field names and semantics verbatim rather than inventing new
/// evidence categories. <c>RecentRunsPerWeek</c> is accepted for provenance
/// symmetry with the existing baseline-input shape but is not itself
/// consumed by the GE volume/long-run formulas (matching the existing Core
/// planner, which also does not use it in the volume/long-run computation).
/// </summary>
internal sealed record LongHorizonGeEntryBaselineInput(
    double? RecentWeeklyVolumeKm,
    double? RecentLongestRunKm,
    int? RecentRunsPerWeek);

/// <summary>One GE week's executed numeric result, prior to workout binding/calendar assignment.</summary>
internal sealed record LongHorizonGeWeekNumericResult(
    int WeekIndex,
    double TotalVolumeKm,
    double LongRunDistanceKm,
    double KeySessionDistanceKm,
    double FirstEasySupportDistanceKm,
    double SecondEasySupportDistanceKm);

/// <summary>
/// Phase 4I.6 — executes the Phase 4I.2 (development progression) and Phase
/// 4I.2A (recovery) numeric policies against an ordered GE structural week
/// sequence (from <see cref="LongHorizonGeStructuralSelector"/>). Reuses
/// <see cref="VolumeSafetyPolicy.Default"/> and <see cref="FourDaySessionDistanceAllocationPolicy"/>
/// verbatim -- introduces zero new numeric constants. Deterministic, pure,
/// no I/O.
/// </summary>
internal static class LongHorizonGeNumericExecutor
{
    public const string PolicyId = "TD-LONG-HORIZON-GE-SAFETY-001";
    public const string RecoveryPolicyId = "TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001";
    public const string PolicyVersion = VolumeSafetyPolicy.PolicyVersion;

    /// <summary>Phase 4I.2A: fixed recovery-week total-volume reduction ratio (0.85 = 15% reduction of the previous non-cutback development peak). Not sourced from <see cref="VolumeSafetyPolicy"/> because it is a GE-specific policy (TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001), not a generic volume-safety constant.</summary>
    public const double RecoveryVolumeRatio = 0.85d;

    /// <summary>Phase 4I.2A: minimum observable total-volume reduction after rounding.</summary>
    public const double MinimumRecoveryReductionKm = 0.5d;

    public static IReadOnlyList<LongHorizonGeWeekNumericResult> Execute(
        IReadOnlyList<LongHorizonGeWeekDescriptor> weeks, LongHorizonGeEntryBaselineInput baseline)
    {
        if (weeks is null || weeks.Count == 0)
            throw new ArgumentException("weeks must be a non-empty ordered GE descriptor sequence.", nameof(weeks));
        if (baseline.RecentWeeklyVolumeKm is not (> 0))
            throw new InvalidOperationException(
                "GE entry baseline requires a positive RecentWeeklyVolumeKm -- no fallback/default is invented; this is the existing authoritative baseline evidence category, reused verbatim, and its absence fails closed.");

        var results = new List<LongHorizonGeWeekNumericResult>(weeks.Count);
        double? previousWeekVolume = null;
        double? previousLongRun = null;
        var priorPeakVolume = baseline.RecentWeeklyVolumeKm.Value;
        var previousWasRecovery = false;

        foreach (var week in weeks)
        {
            double totalVolume;
            if (previousWeekVolume is null)
            {
                // First GE week: the entry baseline itself, unprogressed.
                totalVolume = Round(baseline.RecentWeeklyVolumeKm.Value);
            }
            else if (week.IsRecoveryWeek)
            {
                totalVolume = Round(priorPeakVolume * RecoveryVolumeRatio);
                var reduction = Round(priorPeakVolume - totalVolume);
                if (reduction < MinimumRecoveryReductionKm)
                {
                    totalVolume = Round(priorPeakVolume - MinimumRecoveryReductionKm);
                }
                if (totalVolume >= priorPeakVolume)
                {
                    throw new InvalidOperationException(
                        $"GE week {week.WeekIndex}: recovery volume ({totalVolume}km) did not reduce below the prior peak ({priorPeakVolume}km).");
                }
            }
            else if (previousWasRecovery)
            {
                // Phase 4I.2A postCutbackProgressionBaseline=PREVIOUS_NON_CUTBACK_VOLUME:
                // progress from the prior development PEAK, never from the recovery week's reduced value.
                totalVolume = ApplyDevelopmentProgressionCap(priorPeakVolume);
            }
            else
            {
                totalVolume = ApplyDevelopmentProgressionCap(previousWeekVolume.Value);
            }

            var longRun = week.IsRecoveryWeek
                ? ResolveRecoveryLongRun(totalVolume, previousLongRun!.Value)
                : ResolveDevelopmentLongRun(totalVolume, previousWeekVolume is null ? baseline.RecentLongestRunKm : null);

            var distribution = FourDaySessionDistanceAllocationPolicy.Allocate(totalVolume, longRun);

            results.Add(new LongHorizonGeWeekNumericResult(
                week.WeekIndex, totalVolume, longRun,
                distribution.KeySessionDistanceKm, distribution.FirstEasySupportDistanceKm, distribution.SecondEasySupportDistanceKm));

            if (!week.IsRecoveryWeek)
                priorPeakVolume = Math.Max(priorPeakVolume, totalVolume);

            previousWeekVolume = totalVolume;
            previousLongRun = longRun;
            previousWasRecovery = week.IsRecoveryWeek;
        }

        return results;
    }

    /// <summary>Phase 4I.2: preferred 0.07 ratio increase, absolute 2.5km cap (whichever is smaller), never exceeding the 0.08 hard ceiling. Rounded via the existing 0.5km convention.</summary>
    private static double ApplyDevelopmentProgressionCap(double reference)
    {
        var preferredTarget = reference * (1 + VolumeSafetyPolicy.Default.PreferredMaxWeeklyIncreaseRatio);
        var absoluteCapped = reference + VolumeSafetyPolicy.Default.AbsoluteWeeklyIncrementCapKm;
        var candidate = Round(Math.Min(preferredTarget, absoluteCapped));

        var hardCap = Round(reference * (1 + VolumeSafetyPolicy.Default.HardMaxWeeklyIncreaseRatio));
        if (candidate > hardCap)
            candidate = hardCap;

        return candidate;
    }

    /// <summary>Development-week long run: SelectionShare (0.33) target, clamped to [PreferredMinimumShare, min(PreferredMaximumShare, HardCapShare)] -- the exact rule <c>CatalogVolumeAndLongRunPlanner.BuildLongRunPlan</c> already uses (reused, not reimplemented independently). The very first GE week additionally reconciles against RecentLongestRunKm when present, matching that same existing rule.</summary>
    private static double ResolveDevelopmentLongRun(double weeklyVolumeKm, double? recentLongestRunKm)
    {
        var policy = VolumeSafetyPolicy.Default;
        var target = Round(weeklyVolumeKm * policy.LongRunSelectionShare);
        var lower = Round(weeklyVolumeKm * policy.LongRunPreferredMinimumShare);
        var upper = Round(weeklyVolumeKm * Math.Min(policy.LongRunPreferredMaximumShare, policy.LongRunHardCapShare));

        var unclamped = target;
        if (recentLongestRunKm is > 0)
            unclamped = Math.Min(recentLongestRunKm.Value, target);

        return Round(Math.Clamp(unclamped, lower, upper));
    }

    /// <summary>Phase 4I.2A: RecoveryWeekLongRunKm = Round(RecoveryWeekTotalVolumeKm x 0.33) -- reusing the existing LongRunSelectionShare unmodified.</summary>
    private static double ResolveRecoveryLongRun(double recoveryTotalVolumeKm, double previousLongRunKm)
    {
        var longRun = Round(recoveryTotalVolumeKm * VolumeSafetyPolicy.Default.LongRunSelectionShare);
        if (longRun >= previousLongRunKm)
        {
            throw new InvalidOperationException(
                $"Recovery long run ({longRun}km) did not reduce below the previous development long run ({previousLongRunKm}km).");
        }
        return longRun;
    }

    private static double Round(double value) =>
        Math.Round(value / VolumeSafetyPolicy.Default.RoundingIncrementKm, MidpointRounding.AwayFromZero) * VolumeSafetyPolicy.Default.RoundingIncrementKm;
}
