using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum LongRunProgressionOutcome
{
    Pass,
    Fail,
    NotApplicable,
}

internal sealed record LongRunWeekCheck(
    int WeekNumber,
    double TotalVolumeKm,
    double LongRunKm,
    double ActualShare,
    bool IsTaperWeek,
    bool ExceedsWeeklyVolume,
    bool ViolatesHardCapShare,
    bool ViolatesPreferredShareBand);

internal sealed record LongRunProgressionVerificationResult(
    int TargetWeeks,
    IReadOnlyList<LongRunWeekCheck> WeekChecks,
    LongRunProgressionOutcome Outcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Phase 4G.3B.3, verifier 8 of 9. Checks an already-generated
/// <see cref="CatalogLongRunProgression"/> (the real output of
/// CatalogVolumeAndLongRunPlanner) against the exact stated bounds of the
/// <see cref="VolumeSafetyPolicy"/> that governs it, and against the
/// absolute structural invariant that a long run can never exceed its own
/// week's total volume.
///
/// FACTUAL FINDING (from reading CatalogVolumeAndLongRunPlanner.BuildLongRunPlan
/// before writing this verifier's rules -- do not assume either enforcement
/// or non-enforcement): unlike BuildWeeklyPlan's weekly-volume curve (see
/// TD-VOLUME-CAP-UNENFORCED-001, where HardMaxWeeklyIncreaseRatio/
/// AbsoluteWeeklyIncrementCapKm are threaded through as provenance only and
/// never actually clamp anything), BuildLongRunPlan DOES actively enforce
/// its four share fields: every week's selected long-run distance is
/// computed as `Clamp(unclamped, lower, Math.Min(upper, hardCap))` where
/// lower/upper/hardCap are `weeklyVolume * {PreferredMinimumShare,
/// PreferredMaximumShare, HardCapShare}` -- and the method additionally
/// throws CatalogLongRunHardCapViolationException as a hard runtime
/// fail-safe if the selected value ever exceeds the hard cap by more than a
/// rounding tolerance. Since PreferredMaximumShare (0.36) is strictly less
/// than HardCapShare (0.40), `Math.Min(upper, hardCap)` always resolves to
/// `upper` for this policy's real values, meaning every week is actually
/// clamped into the PREFERRED band itself, not merely the hard cap. This is
/// a genuinely different, positive finding from the weekly-volume case --
/// not a second instance of the same unenforced-gap pattern.
///
/// TAPER FINDING: BuildLongRunPlan has no taper-specific branch. The taper
/// week's long run is computed by the exact same
/// `weeklyVolume * SelectionShare`-then-clamp formula as every other week
/// -- its reduction comes transitively from the taper week's own
/// already-reduced PlannedWeeklyVolumeKm (see BuildWeeklyPlan's taper
/// multiplier), not from a separate taper-specific long-run rule. The
/// planner's own decision-trace text confirms this design intent verbatim:
/// "taper_follows_reduced_weekly_volume_envelope".
///
/// This verifier still independently checks the real numbers against the
/// real policy bounds regardless of the above finding -- the same posture
/// VolumeProgressionVerifier took (verify the actual output, do not merely
/// trust that the planner's internal clamp is bug-free). Pure function of
/// its three inputs: does not call CatalogVolumeAndLongRunPlanner or any
/// loader/resolver. Not called from any live request path.
/// </summary>
internal static class LongRunProgressionVerifier
{
    public static LongRunProgressionVerificationResult Verify(
        CatalogWeeklyVolumePlan volumePlan,
        CatalogLongRunProgression longRunPlan,
        VolumeSafetyPolicy policy)
    {
        if (longRunPlan.Weeks.Count == 0)
        {
            return new(0, [], LongRunProgressionOutcome.NotApplicable,
                ["NOT_APPLICABLE_EMPTY_LONG_RUN_PLAN: no weeks are present to verify."]);
        }

        var taperWeekNumbers = volumePlan.Weeks.Where(w => w.IsTaperWeek).Select(w => w.WeekNumber).ToHashSet();
        var checks = new List<LongRunWeekCheck>();
        var findings = new List<string>();

        foreach (var week in longRunPlan.Weeks.OrderBy(w => w.WeekNumber))
        {
            var isTaper = taperWeekNumbers.Contains(week.WeekNumber);
            var totalVolumeKm = week.PlannedWeeklyVolumeKm;
            var longRunKm = week.PlannedLongRunDistanceKm;
            var actualShare = totalVolumeKm == 0 ? 0d : longRunKm / totalVolumeKm;

            // Absolute structural invariant (task item 4c) -- checked
            // regardless of any share-based enforcement question.
            var exceedsWeeklyVolume = longRunKm > totalVolumeKm;

            var violatesHardCapShare = actualShare > policy.LongRunHardCapShare;
            var violatesPreferredShareBand = actualShare < policy.LongRunPreferredMinimumShare || actualShare > policy.LongRunPreferredMaximumShare;

            checks.Add(new LongRunWeekCheck(
                week.WeekNumber, totalVolumeKm, longRunKm, actualShare, isTaper,
                exceedsWeeklyVolume, violatesHardCapShare, violatesPreferredShareBand));

            if (exceedsWeeklyVolume)
            {
                findings.Add($"EXCEEDS_WEEKLY_VOLUME: week {week.WeekNumber}: LongRunKm={longRunKm}km exceeds TotalVolumeKm={totalVolumeKm}km -- a structural impossibility regardless of any share bound.");
            }

            if (violatesHardCapShare)
            {
                findings.Add($"HARD_CAP_SHARE_VIOLATION: week {week.WeekNumber}: ActualShare={actualShare:0.####} exceeds LongRunHardCapShare={policy.LongRunHardCapShare:0.####} (LongRunKm={longRunKm}km, TotalVolumeKm={totalVolumeKm}km).");
            }
            else if (violatesPreferredShareBand)
            {
                // Soft finding, does not fail the outcome -- mirrors
                // VolumeProgressionVerifier's own Preferred-vs-Hard
                // distinction: a value between the preferred band and the
                // hard cap is not itself a failure, only exceeding the hard
                // cap is. See class doc comment: for this policy's real
                // values PreferredMaximumShare < HardCapShare, so this
                // branch is reachable in principle even though the real
                // planner's own clamp currently prevents it in practice.
                findings.Add($"PREFERRED_SHARE_BAND_DEVIATION_NONFAILING: week {week.WeekNumber}: ActualShare={actualShare:0.####} is outside [{policy.LongRunPreferredMinimumShare:0.####}, {policy.LongRunPreferredMaximumShare:0.####}] but within LongRunHardCapShare={policy.LongRunHardCapShare:0.####} -- reported, does not fail the outcome.");
            }
        }

        var outcome = checks.Any(c => c.ExceedsWeeklyVolume || c.ViolatesHardCapShare)
            ? LongRunProgressionOutcome.Fail
            : LongRunProgressionOutcome.Pass;

        return new(checks[^1].WeekNumber, checks, outcome, findings);
    }
}
