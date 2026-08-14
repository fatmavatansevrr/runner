using RunningApp.Application.RuntimeCatalog.Prescription.Volume;

namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum VolumeProgressionOutcome
{
    Pass,
    Fail,
    NotApplicable,
}

internal sealed record WeeklyTransitionCheck(
    int FromWeek,
    int ToWeek,
    double FromVolumeKm,
    double ToVolumeKm,
    double ActualIncreaseRatio,
    double AllowedMaxIncreaseRatio,
    bool IsTaperTransition,
    bool ViolatesRatio,
    bool ViolatesAbsoluteCapKm);

internal sealed record VolumeProgressionVerificationResult(
    int TargetWeeks,
    IReadOnlyList<WeeklyTransitionCheck> TransitionChecks,
    VolumeProgressionOutcome Outcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Phase 4G.3B.3, verifier 7 of 9. Checks an already-generated
/// <see cref="CatalogWeeklyVolumePlan"/> (the real output of
/// CatalogVolumeAndLongRunPlanner) against the exact stated bounds of the
/// <see cref="VolumeSafetyPolicy"/> that governs it -- not re-deriving new
/// safety rules, not inventing a different standard. Answers a question
/// Phase 4G.3A explicitly left open: "whether the same rules produce a safe
/// curve within 8 weeks" -- mechanically, for any target week count.
///
/// IMPORTANT CAVEAT discovered while implementing this verifier (see
/// VOLUME_PROGRESSION_VERIFIER_FINDINGS section of this phase's report):
/// CatalogVolumeAndLongRunPlanner's own weekly-curve generation
/// (BuildWeeklyPlan) does NOT actually apply HardMaxWeeklyIncreaseRatio or
/// AbsoluteWeeklyIncrementCapKm as a per-week-transition cap anywhere --
/// those two policy fields are threaded into ReachablePeakDecision purely
/// for decision-trace provenance, and the real curve is produced by linear
/// interpolation from the starting volume to a "reachable peak" computed
/// from a completely different, transition-count-scaled formula. This
/// verifier therefore could NOT confirm an "AND vs OR" combination rule
/// for the two caps against real planner logic (the planner does not
/// combine them at all in the actual transition computation) -- it follows
/// the task's own explicit literal instruction instead: ViolatesRatio is
/// independent (ratio alone vs. HardMaxWeeklyIncreaseRatio);
/// ViolatesAbsoluteCapKm requires BOTH the ratio and the absolute km cap to
/// be exceeded.
///
/// Long-run distance is explicitly out of scope here -- see
/// LongRunProgressionVerifier (a separate, later phase). Pure function of
/// its two inputs: does not call CatalogVolumeAndLongRunPlanner,
/// CatalogPeakVolumeBandLoader, or any resolver/loader. Not called from any
/// live request path.
///
/// Enforcement-status note (Phase 4G.3B.7/4G.3B.7.1, TD-VOLUME-CAP-UNENFORCED-001,
/// now CLOSED): HardMaxWeeklyIncreaseRatio and AbsoluteWeeklyIncrementCapKm
/// (the caps this verifier checks against) are informational/provenance-only
/// in the real planner -- CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan
/// never reads either value back to clamp or reject a transition (see the
/// IMPORTANT CAVEAT above, which remains accurate and unchanged). This
/// verifier's own check therefore remains the sole place these two bounds
/// are actually verified today, independent of any planner enforcement.
/// Phase 4G.3B.7's decision audit (PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md)
/// proved algebraically that TEN_K_MASTER v6's real curves stay below both
/// bounds by construction, not by coincidence -- see that document for the
/// full proof and scope conditions.
/// </summary>
internal static class VolumeProgressionVerifier
{
    public static VolumeProgressionVerificationResult Verify(CatalogWeeklyVolumePlan volumePlan, VolumeSafetyPolicy policy)
    {
        var weeks = volumePlan.Weeks.OrderBy(w => w.WeekNumber).ToList();

        if (weeks.Count < 2)
        {
            return new(weeks.Count == 1 ? weeks[0].WeekNumber : 0, [], VolumeProgressionOutcome.NotApplicable,
                ["NOT_APPLICABLE_FEWER_THAN_TWO_WEEKS: fewer than two weeks are present, so no week-to-week transition exists to verify."]);
        }

        var checks = new List<WeeklyTransitionCheck>();
        var findings = new List<string>();

        for (var i = 0; i < weeks.Count - 1; i++)
        {
            var from = weeks[i];
            var to = weeks[i + 1];
            var changeKm = to.PlannedWeeklyVolumeKm - from.PlannedWeeklyVolumeKm;
            var actualRatio = from.PlannedWeeklyVolumeKm == 0 ? 0d : changeKm / from.PlannedWeeklyVolumeKm;
            var isTaper = to.IsTaperWeek;

            bool violatesRatio;
            bool violatesAbsoluteCap;
            double allowedMaxIncreaseRatio;

            if (isTaper)
            {
                allowedMaxIncreaseRatio = policy.TaperVolumeMultiplier - 1d;
                var expectedTaperVolumeKm = from.PlannedWeeklyVolumeKm * policy.TaperVolumeMultiplier;
                var deviationKm = Math.Abs(to.PlannedWeeklyVolumeKm - expectedTaperVolumeKm);
                violatesRatio = deviationKm > policy.RoundingIncrementKm;
                violatesAbsoluteCap = false; // the absolute weekly-increment cap governs increases, not the taper reduction rule.

                if (violatesRatio)
                {
                    findings.Add($"TAPER_MULTIPLIER_VIOLATION: week {from.WeekNumber}->{to.WeekNumber}: actual={to.PlannedWeeklyVolumeKm}km, expected={expectedTaperVolumeKm}km (FromVolumeKm={from.PlannedWeeklyVolumeKm}km * TaperVolumeMultiplier={policy.TaperVolumeMultiplier}), deviation={deviationKm}km exceeds RoundingIncrementKm={policy.RoundingIncrementKm}km.");
                }
            }
            else
            {
                allowedMaxIncreaseRatio = policy.HardMaxWeeklyIncreaseRatio;
                violatesRatio = actualRatio > policy.HardMaxWeeklyIncreaseRatio;
                violatesAbsoluteCap = changeKm > policy.AbsoluteWeeklyIncrementCapKm;

                if (violatesRatio || violatesAbsoluteCap)
                {
                    findings.Add($"WEEKLY_INCREASE_VIOLATION: week {from.WeekNumber}->{to.WeekNumber}: {from.PlannedWeeklyVolumeKm}km -> {to.PlannedWeeklyVolumeKm}km (+{changeKm}km), ratio={actualRatio:0.####} exceeds HardMaxWeeklyIncreaseRatio={policy.HardMaxWeeklyIncreaseRatio:0.####}" +
                        (violatesAbsoluteCap ? $"; absolute increase also exceeds AbsoluteWeeklyIncrementCapKm={policy.AbsoluteWeeklyIncrementCapKm}km." : "."));
                }
            }

            checks.Add(new WeeklyTransitionCheck(
                from.WeekNumber, to.WeekNumber, from.PlannedWeeklyVolumeKm, to.PlannedWeeklyVolumeKm,
                actualRatio, allowedMaxIncreaseRatio, isTaper, violatesRatio, violatesAbsoluteCap));
        }

        var outcome = checks.Any(c => c.ViolatesRatio || c.ViolatesAbsoluteCapKm)
            ? VolumeProgressionOutcome.Fail
            : VolumeProgressionOutcome.Pass;

        return new(weeks[^1].WeekNumber, checks, outcome, findings);
    }
}
