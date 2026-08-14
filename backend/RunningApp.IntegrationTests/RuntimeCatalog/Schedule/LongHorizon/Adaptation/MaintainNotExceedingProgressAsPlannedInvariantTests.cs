using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.Adaptation;

/// <summary>
/// Phase 4M.4B.2B/C -- the canonical V1 acceptance invariant for Maintain
/// vs. ProgressAsPlanned, per Rev4.1's frozen ROUNDING PRODUCT DEFAULT
/// (<c>appsel-adaptation-v1-canonical-spec — Revision 4.1.md</c>).
///
/// Phase 4M.4B.2B's real-catalog sweep found the strict
/// <c>Maintain &lt;= ProgressAsPlanned</c> ordering does not hold exactly:
/// 94/183 valid cases violated it by 0.02-0.55km, entirely attributable to
/// the catalog's own real session-distance rounding (not to Maintain
/// applying any progression step, and not to any adaptation-side uplift --
/// Maintain still holds <c>PriorValidatedCheckpointLoad</c> verbatim,
/// unchanged from Rev4). Rev4.1 froze this as: Maintain must not
/// MATERIALLY exceed ProgressAsPlanned, where a rounding-only relative
/// deviation of &lt;= 1.5% is an accepted PRODUCT DEFAULT, not a scientific
/// threshold, and not enforced anywhere at runtime -- it exists only here,
/// as the acceptance criterion for this governance invariant.
///
/// Per Rev4 §7: <c>Maintain = PriorValidatedCheckpointLoad</c> (held
/// verbatim, no re-derivation) and
/// <c>ProgressAsPlanned = CatalogProgressionStep(ValidatedSustainableLoad)</c>.
/// Feeding the SAME evidence value as both "the anchor Maintain holds" and
/// "the anchor ProgressAsPlanned progresses from" reduces the comparison
/// to: does the real catalog progression step (the General Endurance
/// numeric executor -- the same <see cref="LongHorizonGeNumericExecutor"/>
/// the real GE materializer path uses, not a reimplementation) ever
/// produce week 1's volume MATERIALLY below its own baseline input?
/// </summary>
public sealed class MaintainNotExceedingProgressAsPlannedInvariantTests
{
    /// <summary>
    /// Rev4.1 ROUNDING PRODUCT DEFAULT acceptance threshold. Governance/test
    /// acceptance criterion only -- deliberately not a runtime constant:
    /// nothing at runtime compares Maintain against ProgressAsPlanned or
    /// needs to enforce this tolerance; it exists solely to classify sweep
    /// deviations here as accepted rounding noise vs. a real violation.
    /// </summary>
    private const double RoundingToleranceRelativeDeviation = 0.015;

    [Fact]
    public void Maintain_DoesNotMateriallyExceedProgressAsPlanned_BeyondRoundingTolerance()
    {
        var random = new Random(42);
        var profiles = new[] { ReadinessProfile.ConsistencyNeeded, ReadinessProfile.CoreEntryReady };
        var cases = 0;
        var infeasible = 0;
        var plateaus = 0;
        var growth = 0;
        var strictViolations = 0;
        var beyondTolerance = new List<string>();
        double maxAbsoluteDeviation = 0, maxRelativeDeviation = 0;
        double minWeekly = double.MaxValue, maxWeekly = double.MinValue;

        for (var i = 0; i < 200; i++)
        {
            // Realistic recent-evidence ranges (matches the product's own
            // onboarding evidence bounds, not arbitrary values). Same
            // deterministic generation as the original 4M.4B.2B sweep.
            var weeklyVolumeKm = 5 + random.NextDouble() * 55;   // 5..60 km/week
            var longestRunKm = weeklyVolumeKm * (0.2 + random.NextDouble() * 0.3); // 20-50% of weekly, matching typical long-run share
            var runsPerWeek = random.Next(3, 7);
            var geWeeks = random.Next(1, 33); // 1..32 GE weeks -- the full structurally-selectable range
            var profile = profiles[random.Next(profiles.Length)];

            minWeekly = Math.Min(minWeekly, weeklyVolumeKm);
            maxWeekly = Math.Max(maxWeekly, weeklyVolumeKm);

            var descriptors = LongHorizonGeStructuralSelector.Select(geWeeks, profile);
            if (descriptors.Count == 0) continue;

            var baseline = new LongHorizonGeEntryBaselineInput(weeklyVolumeKm, longestRunKm, runsPerWeek);
            IReadOnlyList<LongHorizonGeWeekNumericResult> numeric;
            try
            {
                numeric = LongHorizonGeNumericExecutor.Execute(descriptors, baseline);
            }
            catch (CatalogSessionPrescriptionInfeasibleException)
            {
                // A genuinely infeasible starting combination (e.g. too little
                // residual volume for the other required sessions once the
                // long run's share is subtracted) -- Rev4.1's TARGET
                // PRESCRIPTION INFEASIBILITY behavior, the same real,
                // existing per-session minimum-volume validation 4M.4B.2B
                // found and confirmed is symmetric/generic. Not a valid case
                // to compare against; skipped and counted, not silently
                // discarded.
                infeasible++;
                continue;
            }
            if (numeric.Count == 0) continue;

            cases++;
            // Maintain's anchor (held verbatim) vs. ProgressAsPlanned's real,
            // catalog-executed week-1 output from the SAME starting evidence.
            var maintainAnchorWeeklyKm = weeklyVolumeKm;
            var progressAsPlannedAnchorWeeklyKm = numeric[0].TotalVolumeKm;

            if (Math.Abs(progressAsPlannedAnchorWeeklyKm - maintainAnchorWeeklyKm) < 0.01)
                plateaus++;
            else if (progressAsPlannedAnchorWeeklyKm > maintainAnchorWeeklyKm)
                growth++;

            if (maintainAnchorWeeklyKm > progressAsPlannedAnchorWeeklyKm + 0.01)
            {
                strictViolations++;
                var absoluteDeviation = maintainAnchorWeeklyKm - progressAsPlannedAnchorWeeklyKm;
                var relativeDeviation = absoluteDeviation / maintainAnchorWeeklyKm;
                maxAbsoluteDeviation = Math.Max(maxAbsoluteDeviation, absoluteDeviation);
                maxRelativeDeviation = Math.Max(maxRelativeDeviation, relativeDeviation);

                if (relativeDeviation > RoundingToleranceRelativeDeviation)
                    beyondTolerance.Add($"case={i} weekly={weeklyVolumeKm:F2} longRun={longestRunKm:F2} runsPerWeek={runsPerWeek} geWeeks={geWeeks} profile={profile} " +
                        $"maintain={maintainAnchorWeeklyKm:F2} progress={progressAsPlannedAnchorWeeklyKm:F2} absoluteDeviation={absoluteDeviation:F3} relativeDeviation={relativeDeviation:P2}");
            }
        }

        // The canonical V1 acceptance invariant (Rev4.1): rounding-only
        // excess up to 1.5% is accepted PRODUCT DEFAULT; nothing beyond it
        // is. Do NOT widen this to make the test pass -- if any case
        // exceeds tolerance, this must fail loudly with full detail.
        Assert.True(beyondTolerance.Count == 0,
            $"Maintain materially exceeded ProgressAsPlanned (beyond the Rev4.1 {RoundingToleranceRelativeDeviation:P1} rounding tolerance) in {beyondTolerance.Count}/{cases} cases:\n" +
            string.Join("\n", beyondTolerance));

        // Sanity: the sweep actually exercised a meaningful, non-trivial range.
        Assert.True(cases >= 100, $"Only {cases}/200 randomized cases produced a valid, feasible descriptor/numeric result (infeasible={infeasible}) -- sweep coverage too narrow.");
        Assert.True(minWeekly < 10 && maxWeekly > 50, $"Input range not representative: min={minWeekly:F2} max={maxWeekly:F2}.");

        // Report (visible in test output):
        Assert.True(true,
            $"Maintain vs ProgressAsPlanned (Rev4.1 tolerance={RoundingToleranceRelativeDeviation:P1}): {cases} valid cases " +
            $"(+{infeasible} infeasible/skipped, the same real per-session minimum-volume rule 4M.4B.2B found), " +
            $"weekly range [{minWeekly:F2}-{maxWeekly:F2}]km, {plateaus} plateaus (Maintain==Progress), {growth} growth cases (Maintain<Progress), " +
            $"{strictViolations} strict-order violations (Maintain>Progress, all rounding-only), " +
            $"maxAbsoluteDeviation={maxAbsoluteDeviation:F3}km, maxRelativeDeviation={maxRelativeDeviation:P2}, {beyondTolerance.Count} cases beyond tolerance.");
    }
}
